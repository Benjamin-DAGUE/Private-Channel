using Google.Protobuf;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PrivateChannel.Back.Core;
using PrivateChannel.DataModel;
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
    ///     Service de gestion des bannies.
    /// </summary>
    private BanService _BanService;

    /// <summary>
    ///     <see cref="PrivateChannelContext"/> factory.
    /// </summary>
    private readonly IDbContextFactory<PrivateChannelContext> _DbContextFactory;

    #endregion

    #region Constructors

    /// <summary>
    ///     Initialise une nouvelle instance de la classe <see cref="PrivateNoteService"/>.
    /// </summary>
    /// <param name="logger">Journal à utiliser.</param>
    /// <param name="dataProtectionProvider">Fournisseur de protectionn des données.</param>
    /// <param name="banService">Service de gestion des bannies.</param>
    public PrivateNoteService(ILogger<PrivateNoteService> logger, BanService banService, IDbContextFactory<PrivateChannelContext> dbContextFactory)
    {
        _Logger = logger;
        _BanService = banService;
        _DbContextFactory = dbContextFactory;
    }

    #endregion

    #region Methods

    public override async Task<CreateNoteResponse> CreateNote(CreateNoteRequest request, ServerCallContext context)
    {
        if (request.CipherText.Length == 0 || request.CipherText.Length > 10000)
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

        using PrivateChannelContext dbContext = await _DbContextFactory.CreateDbContextAsync();

        for (int i = 0; i < 10; i++)
        {
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();

            byte[] randomNoteId = new byte[16];
            rng.GetBytes(randomNoteId);

            Guid guid = new Guid(randomNoteId);

            List<Note> notes = await dbContext.Notes.Where(n => n.ExpirationDateTime <= DateTime.Now).ToListAsync();

            if (notes.Any())
            {
                dbContext.Notes.RemoveRange(notes);
                await dbContext.SaveChangesAsync();
            }

            using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();

            if (dbContext.Notes.Any(n => n.Id == guid) == false)
            {
                try
                {
                    TimeSpan delay = TimeSpan.FromMinutes(Math.Max(1, Math.Min(5040, request.MinutesAvailable)));

                    await dbContext.Notes.AddAsync(new Note()
                    {
                        Id = guid,
                        ExpirationDateTime = DateTime.Now.Add(delay),
                        CipherText = request.CipherText.ToArray(),
                        AuthTag = request.AuthTag.ToArray(),
                        IV = request.IV.ToArray(),
                        Salt = request.Salt.ToArray(),
                        RemainingUnlockAttempts = Math.Min(100, Math.Max(1, request.MaxUnlockAttempts))
                    });

                    await dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new CreateNoteResponse()
                    {
                        Id = ByteString.CopyFrom(randomNoteId)
                    };
                }
                catch (Exception ex)
                {
                    _Logger.LogError("Unable to save note {nl}{ex}:", Environment.NewLine, ex);
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            else
            {
                transaction.Rollback();
            }

            Thread.Sleep(50);
        }

        throw new Exception("Unable to create unique note id");
    }

    public override async Task<ReadNoteResponse> ReadNote(ReadNoteRequest request, ServerCallContext context)
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

        using PrivateChannelContext dbContext = await _DbContextFactory.CreateDbContextAsync();
        Note? note = await dbContext.Notes.FirstOrDefaultAsync(n => n.Id == guid);

        if (note != null)
        {
            try
            {
                if (note.ExpirationDateTime <= DateTime.Now)
                {
                    dbContext.Remove(note);
                    await dbContext.SaveChangesAsync();
                    throw new Exception("Note expired");
                }

                note.RemainingUnlockAttempts--;
                if (note.RemainingUnlockAttempts <= 0)
                {
                    dbContext.Remove(note);
                }
                await dbContext.SaveChangesAsync();

                byte[] plainBytes = new byte[note.CipherText.Length];
                byte[] key = Rfc2898DeriveBytes.Pbkdf2(request.Password, note.Salt, 5000, HashAlgorithmName.SHA256, 32);

                using var aes = new AesGcm(key);
                aes.Decrypt(note.IV, note.CipherText, note.AuthTag, plainBytes);

                if (note.RemainingUnlockAttempts > 0)
                {
                    dbContext.Notes.Remove(note);
                    await dbContext.SaveChangesAsync(); 
                }

                return new ReadNoteResponse()
                {
                    Message = Encoding.UTF8.GetString(plainBytes)
                };
            }
            catch (Exception ex)
            {
                _Logger.LogError("Unable to read note {nl}{ex}:", Environment.NewLine, ex);
                _BanService.AddStrike(context);
                throw;
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