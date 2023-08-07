using Google.Protobuf;
using Grpc.Core;
using PrivateChannel.Back.Core;
using System.Security.Cryptography;
using System.Text;

namespace PrivateChannel.Back.Services;

public class PrivateNoteService : PrivateNoteSvc.PrivateNoteSvcBase
{
    #region Fields

    /// <summary>
    ///     Représente le journal.
    /// </summary>
    private readonly ILogger<PrivateNoteService> _Logger;

    /// <summary>
    ///     Verrou d'accès aux dictionnaires des notes protégés.
    /// </summary>
    private static object _NotesLocker = new object();

    /// <summary>
    ///     Dictionnaire des notes protégés.
    /// </summary>
    public static Dictionary<Guid, Note> _ProtectedNotes = new Dictionary<Guid, Note>();

    /// <summary>
    ///     Service de gestion des bannies.
    /// </summary>
    private BanService _BanService;

    #endregion

    #region Constructors

    /// <summary>
    ///     Initialise une nouvelle instance de la classe <see cref="PrivateNoteService"/>.
    /// </summary>
    /// <param name="logger">Journal à utiliser.</param>
    /// <param name="dataProtectionProvider">Fournisseur de protectionn des données.</param>
    /// <param name="banService">Service de gestion des bannies.</param>
    public PrivateNoteService(ILogger<PrivateNoteService> logger, BanService banService)
    {
        _Logger = logger;
        _BanService = banService;
    }

    #endregion

    #region Methods

    public override Task<CreateNoteResponse> CreateNote(CreateNoteRequest request, ServerCallContext context)
    {
        if (request.CipherText.Length == 0)
        {
            _BanService.AddStrike(context);
            throw new ArgumentNullException(nameof(request.CipherText));
        }
        if (request.AuthTag.Length == 0)
        {
            _BanService.AddStrike(context);
            throw new ArgumentNullException(nameof(request.AuthTag));
        }
        if (request.IV.Length == 0)
        {
            _BanService.AddStrike(context);
            throw new ArgumentNullException(nameof(request.IV));
        }
        if (request.Salt.Length == 0)
        {
            _BanService.AddStrike(context);
            throw new ArgumentNullException(nameof(request.Salt));
        }

        int noteCount = 0;

        for (int i = 0; i < 10; i++)
        {
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();

            byte[] randomNoteId = new byte[16];
            rng.GetBytes(randomNoteId);

            Guid guid = new Guid(randomNoteId);

            bool guidAlreadyExisting = true;

            lock (_NotesLocker)
            {
                _ProtectedNotes.Where(kvp => kvp.Value.ExpirationDateTime <= DateTime.Now).ToList().ForEach(kvp => _ProtectedNotes.Remove(kvp.Key));

                if (_ProtectedNotes.ContainsKey(guid) == false)
                {
                    guidAlreadyExisting = false;
                    noteCount = _ProtectedNotes.Count;
                }
            }

            if (guidAlreadyExisting == false)
            {
                try
                {
                    TimeSpan delay = TimeSpan.FromMinutes(Math.Max(1, Math.Min(5040, request.MinutesAvailable)));

                    lock (_NotesLocker)
                    {
                        _ProtectedNotes.Add(guid, new Note()
                        {
                            Id = guid,
                            CipherText = request.CipherText.ToArray(),
                            AuthTag = request.AuthTag.ToArray(),
                            IV = request.IV.ToArray(),
                            Salt = request.Salt.ToArray(),
                            ExpirationDateTime = DateTime.Now.Add(delay),
                        });
                    }

                    return Task.FromResult(new CreateNoteResponse()
                    {
                        Id = ByteString.CopyFrom(randomNoteId)
                    });
                }
                catch (Exception ex)
                {
                    _Logger.LogError("Unable to save note {nl}{ex}:", Environment.NewLine, ex);
                    throw;
                }
            }

            Thread.Sleep(50);
        }

        _Logger.LogCritical("Unable to create a new id for a note. Current note count: {0}", noteCount);
        throw new Exception("Unable to create unique note id");
    }

    public override Task<ReadNoteResponse> ReadNote(ReadNoteRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            _BanService.AddStrike(context);
            throw new ArgumentNullException(nameof(request.Password));
        }

        byte[] id = request.Id.ToArray();

        if (id.Length != 16)
        {
            _BanService.AddStrike(context);
            throw new ArgumentNullException(nameof(request.Id));
        }

        Guid guid = new Guid(id);
        Note? note = null;

        lock (_NotesLocker)
        {
            if (_ProtectedNotes.ContainsKey(guid))
            {
                note = _ProtectedNotes[guid];
            }
        }

        if (note != null)
        {
            try
            {
                byte[] plainBytes = new byte[note.CipherText.Length];
                byte[] key = Rfc2898DeriveBytes.Pbkdf2(request.Password, note.Salt, 5000, HashAlgorithmName.SHA256, 32);

                using var aes = new AesGcm(key);
                aes.Decrypt(note.IV, note.CipherText, note.AuthTag, plainBytes);

                lock (_NotesLocker)
                {
                    _ProtectedNotes.Remove(guid);
                }

                return Task.FromResult(new ReadNoteResponse()
                {
                    Message = Encoding.UTF8.GetString(plainBytes)
                });
            }
            catch (Exception ex)
            {
                if (ex.Message.StartsWith("The payload expired"))
                {
                    _Logger.LogInformation("Note expired: {id}", guid);
                    lock (_NotesLocker)
                    {
                        _ProtectedNotes.Remove(guid);
                    }
                }
                else
                {
                    _Logger.LogError("Unable to read note {nl}{ex}:", Environment.NewLine, ex);
                    _BanService.AddStrike(context);
                }

                throw new Exception("Unalbe to read note.");
            }
        }
        else
        {
            _Logger.LogInformation("Unable to find note for id: {id}", guid);
            _BanService.AddStrike(context);
            throw new Exception("Unalbe to read note.");
        }
    }

    #endregion
}