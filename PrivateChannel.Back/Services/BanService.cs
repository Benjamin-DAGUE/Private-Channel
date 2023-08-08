using Grpc.Core;
using System.Collections.Concurrent;

namespace PrivateChannel.Back.Services;

/// <summary>
///     Service de gestion des bannies.
/// </summary>
public class BanService
{
    #region Fields

    /// <summary>
    ///     Chemin du fichier.
    /// </summary>
    private readonly string? _BanListFilePath;

    /// <summary>
    ///     Dictionnaire des bannies.
    /// </summary>
    private readonly Dictionary<string, DateTime> _BannedIPs = new Dictionary<string, DateTime>();

    /// <summary>
    ///     Verrou d'accès au dictionnaire des IP bannies.
    /// </summary>
    private object _BannedIPsLocker = new object();

    /// <summary>
    ///     Verrou d'accès pour l'écriture du fichier.
    /// </summary>
    private object _FileWriterLocker = new object();

    /// <summary>
    ///     Date et heure de la dernièr esauvegarde de la liste.
    /// </summary>
    private DateTime? _LastSave = null;

    /// <summary>
    ///     Dictionnaire des avertissements.
    /// </summary>
    private readonly ConcurrentDictionary<string, int> _Strikes = new ConcurrentDictionary<string, int>();

    private DateTime? _LastStrikesClear = null;

    /// <summary>
    ///     Max strike before ban.
    /// </summary>
    private readonly int _MaxStrikesCount = 20;

    /// <summary>
    ///     Dictionnaire d'utilsiation.
    /// </summary>
    private readonly ConcurrentDictionary<string, int> _Usages = new ConcurrentDictionary<string, int>();

    private DateTime? _LastUsageClear = null;

    /// <summary>
    ///     Max usage before ban.
    /// </summary>
    private readonly int _MaxUsagesCountPerHour = 200;

    /// <summary>
    ///     White list of all Ip.
    /// </summary>
    private readonly string[] _WhiteList;

    #endregion

    #region Constructors

    /// <summary>
    ///     Initialise une nouvelle instance de la classe <see cref="BanService"/>.
    /// </summary>
    /// <param name="banListFilePath">Chemin du fichier à charger. Si null, la liste ne sera pas sauvegardée.</param>
    public BanService(string? banListFilePath, int maxStrikesCount, int maxUsagesCountPerHour, params string[] whitelists)
    {
        _BanListFilePath = banListFilePath;
        _MaxStrikesCount = maxStrikesCount;
        _MaxUsagesCountPerHour = maxUsagesCountPerHour;
        _WhiteList = whitelists;

        if (File.Exists(banListFilePath))
        {
            string json = File.ReadAllText(banListFilePath);
            _BannedIPs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json) ?? new Dictionary<string, DateTime>();
        }
        else
        {
            _BannedIPs = new Dictionary<string, DateTime>();
        }
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Ajoute un avertissement à l'adresse IP du contexte d'appel.
    ///     L'adresse IP est bannie lorsque le seuil maximum est atteint.
    /// </summary>
    /// <param name="context">Contexte d'appel pour lequel ajouter un avertissement.</param>
    public void AddStrike(ServerCallContext context)
    {
        string? ipAddress = context.GetHttpContext().Connection.RemoteIpAddress?.ToString();
        if (ipAddress != null)
        {
            AddStrike(ipAddress);
        }
    }

    /// <summary>
    ///     Ajoute un avertissement à l'adresse IP.
    ///     L'adresse IP est bannie lorsque le seuil maximum est atteint.
    /// </summary>
    /// <param name="ip">Addresse IP à laquelle ajouter un avertissement.</param>
    public void AddStrike(string ip)
    {
        if (_LastStrikesClear == null || _LastStrikesClear.Value <= DateTime.Now.AddDays(-1))
        {
            _LastStrikesClear = DateTime.Now;
            _Strikes.Clear();
        }

        if (_WhiteList.Contains(ip) == false)
        {
            if (_Strikes.ContainsKey(ip) == false)
            {
                _Strikes.TryAdd(ip, 1);
            }
            else
            {
                _Strikes[ip] = _Strikes[ip] + 1;

                if (_Strikes[ip] >= _MaxStrikesCount)
                {
                    BanIp(ip);
                    _Strikes[ip] = 0;
                }
            }
        }
    }

    /// <summary>
    ///     Applique un ban à l'IP.
    /// </summary>
    /// <param name="ip">IP à bannir.</param>
    public void BanIp(string ip)
    {
        if (_WhiteList.Contains(ip) == false)
        {
            lock (_BannedIPsLocker)
            {
                if (!_BannedIPs.ContainsKey(ip))
                {
                    _BannedIPs.Add(ip, DateTime.Now.AddDays(1));
                }
                else
                {
                    _BannedIPs[ip] = DateTime.Now.AddDays(1);
                }

                if (string.IsNullOrWhiteSpace(_BanListFilePath) == false)
                {
                    _ = Task.Run(() =>
                    {
                        lock (_FileWriterLocker)
                        {
                            if (_LastSave == null || _LastSave <= DateTime.Now.AddMinutes(-5))
                            {
                                Dictionary<string, DateTime>? copy = null;

                                lock (_BannedIPs)
                                {
                                    copy = new Dictionary<string, DateTime>(_BannedIPs);
                                }

                                string json = System.Text.Json.JsonSerializer.Serialize(copy);
                                File.WriteAllText(_BanListFilePath, json);

                                _LastSave = DateTime.Now;
                            }
                            else
                            {
                                return;
                            }
                        }
                    });
                }
            }
        }
    }

    /// <summary>
    ///     Détermine si l'adresse IP est bannie.
    /// </summary>
    /// <param name="ip">IP à tester.</param>
    /// <returns>Retourne vrai si l'IP est bannie.</returns>
    public bool IsBanned(string ip)
    {
        bool result = false;

        lock (_BannedIPsLocker)
        {
            if (_BannedIPs.ContainsKey(ip))
            {
                if (_BannedIPs[ip] < DateTime.Now)
                {
                    _BannedIPs.Remove(ip);
                }
                else
                {
                    result = true;
                }
            }
        }

        return result;
    }

    public void AddUsage(ServerCallContext context)
    {
        string? ipAddress = context.GetHttpContext().Connection.RemoteIpAddress?.ToString();
        if (ipAddress != null)
        {
            AddStrike(ipAddress);
        }
    }

    public void AddUsage(string ip)
    {
        if (_LastUsageClear == null || _LastUsageClear.Value <= DateTime.Now.AddHours(-1))
        {
            _LastUsageClear = DateTime.Now;
            _Usages.Clear();
        }

        if (_WhiteList.Contains(ip) == false)
        {
            if (_Usages.ContainsKey(ip) == false)
            {
                _Usages.TryAdd(ip, 1);
            }
            else
            {
                _Usages[ip] = _Usages[ip] + 1;

                if (_Usages[ip] >= _MaxUsagesCountPerHour)
                {
                    BanIp(ip);
                }
            }
        }
    }

    public Dictionary<string, DateTime> GetBanList()
    {
        Dictionary<string, DateTime> banList = new Dictionary<string, DateTime>();

        lock (_BannedIPsLocker)
        {
            banList = new Dictionary<string, DateTime>(_BannedIPs);
        }

        return banList;
    }

    #endregion
}
