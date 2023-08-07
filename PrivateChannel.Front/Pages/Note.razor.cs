using Google.Protobuf;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Security.Cryptography;

namespace PrivateChannel.Front.Pages;

public class EncryptedNote
{
    public List<byte> CipherText { get; set; } = new List<byte>();
    public List<byte> AuthTag { get; set; } = new List<byte>();
    public List<byte> IV { get; set; } = new List<byte>();
    public List<byte> Salt { get; set; } = new List<byte>();
}

public partial class Note
{
    #region Properties

    [Parameter]
    public Guid? NoteId { get; set; }

    public string? NoteLink { get; set; }

    public string? Message { get; set; }
    public string? Password { get; set; }
    public int HoursAvailable { get; set; } = 24;

    #endregion

    #region Methods

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        if (NoteId == null)
        {
            Password = GenerateSecurePassword(12);
            ShowPasword();
        }
        else
        {
            Password = null;
            HidePassword();
        }
    }

    private async Task SendNote()
    {
        if (string.IsNullOrWhiteSpace(Message) == false && string.IsNullOrWhiteSpace(Password) == false)
        {
            try
            {
                EncryptedNote cipherData = await JsInterop.InvokeAsync<EncryptedNote>("encryptWithPassword", Message, Password);

                CreateNoteResponse response = await Client.CreateNoteAsync(new CreateNoteRequest()
                {
                    CipherText = ByteString.CopyFrom(cipherData.CipherText.ToArray()),
                    AuthTag = ByteString.CopyFrom(cipherData.AuthTag.ToArray()),
                    IV = ByteString.CopyFrom(cipherData.IV.ToArray()),
                    Salt = ByteString.CopyFrom(cipherData.Salt.ToArray()),
                    MinutesAvailable = HoursAvailable * 60,
                });

                Guid noteId = new Guid(response.Id.ToByteArray());
                NoteLink = NavigationManager.Uri + noteId.ToString();

                Snackbar.Add("Note sent, copy note link and share it!", MudBlazor.Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Unable to send note, please refresh and try again.", MudBlazor.Severity.Error);
            }
        }
    }

    private async Task ReadNote()
    {
        if (NoteId != null && string.IsNullOrWhiteSpace(Message) == true && string.IsNullOrWhiteSpace(Password) == false)
        {
            try
            {
                ReadNoteResponse response = await Client.ReadNoteAsync(new ReadNoteRequest()
                {
                    Id = ByteString.CopyFrom(NoteId.Value.ToByteArray()),
                    Password = Password
                });

                Message = response.Message;
                Snackbar.Add("Note unlocked!", MudBlazor.Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Unable to read note because it expired or password is wrong.", MudBlazor.Severity.Error);
            }

            Password = null;
        }
    }

    private async Task CopyNoteToClipboard()
    {
        await JsInterop.InvokeVoidAsync("navigator.clipboard.writeText", Message);
        Snackbar.Add("Note content copied to clipboard!");
    }

    private async Task CopyNoteLinkToClipboard()
    {
        await JsInterop.InvokeVoidAsync("navigator.clipboard.writeText", NoteLink);
        Snackbar.Add("Note link copied to clipboard!");
    }

    public static string GenerateSecurePassword(int length)
    {
        string chars = "abcdefghijklmnopqrstuvwxyz0123456789+-*/=!";
        char[] password = new char[length];
        byte[] buffer = new byte[length * 8];
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();

        rng.GetBytes(buffer);

        for (int i = 0; i < length; i++)
        {
            uint val = BitConverter.ToUInt32(buffer, i * 8);
            password[i] = chars[(int)(val % (uint)chars.Length)];
        }
        return new string(password);
    }

    #endregion
}
