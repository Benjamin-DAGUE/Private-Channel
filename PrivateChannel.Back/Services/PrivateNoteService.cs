using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.DataProtection;
using PrivateChannel.Back.Core;
using System.Security.Cryptography;

namespace PrivateChannel.Back.Services;

public class PrivateNoteService : PrivateNoteSvc.PrivateNoteSvcBase
{
    #region Fields

    /// <summary>
    ///     Fournisseur de protection des données.
    /// </summary>
    private readonly IDataProtectionProvider _DataProtectionProvider;

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
    public PrivateNoteService(ILogger<PrivateNoteService> logger, IDataProtectionProvider dataProtectionProvider, BanService banService)
    {
        _Logger = logger;
        _DataProtectionProvider = dataProtectionProvider;
        _BanService = banService;
    }

    #endregion

    #region Methods

    public override Task<CreateNoteResponse> CreateNote(CreateNoteRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            _BanService.AddStrike(context);
            throw new ArgumentNullException(nameof(request.Password));
        }
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            _BanService.AddStrike(context);
            throw new ArgumentNullException(nameof(request.Message));
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
                    IDataProtector dataProtector = _DataProtectionProvider.CreateProtector("Private-Channel.Back", "v1", $"{guid}-{request.Password}");
                    ITimeLimitedDataProtector timeLimitedDataProtector = dataProtector.ToTimeLimitedDataProtector();
                    TimeSpan delay = TimeSpan.FromMinutes(Math.Max(1, Math.Min(5040, request.MinutesAvailable)));

                    string protectedMessage = timeLimitedDataProtector.Protect(request.Message, delay);

                    lock (_NotesLocker)
                    {
                        _ProtectedNotes.Add(guid, new Note()
                        {
                            Id = guid,
                            ExpirationDateTime = DateTime.Now.Add(delay),
                            Message = protectedMessage
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
                IDataProtector dataProtector = _DataProtectionProvider.CreateProtector("Private-Channel.Back", "v1", $"{guid}-{request.Password}");
                ITimeLimitedDataProtector timeLimitedDataProtector = dataProtector.ToTimeLimitedDataProtector();
                
                string message = timeLimitedDataProtector.Unprotect(note.Message);

                lock (_NotesLocker)
                {
                    _ProtectedNotes.Remove(guid);
                }

                return Task.FromResult(new ReadNoteResponse()
                {
                    Message = message,
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