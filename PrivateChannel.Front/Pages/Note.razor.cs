using Google.Protobuf;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MudBlazor;
using PrivateChannel.Front.Models;
using System.Security.Cryptography;

namespace PrivateChannel.Front.Pages;

public partial class Note
{
    #region Properties

    /// <summary>
    ///     Get or set the note identifier.
    /// </summary>
    [Parameter]
    public Guid? NoteId { get; set; }

    /// <summary>
    ///     Get or set the note uri.
    /// </summary>
    private string? NoteLink { get; set; }

    /// <summary>
    ///     Get or set the message.
    /// </summary>
    private string? Message { get; set; }

    /// <summary>
    ///     Get or set how many hours the message is available on server.
    /// </summary>
    private int HoursAvailable { get; set; } = 72;

    #region Password management

    /// <summary>
    ///     Get or set the note password.
    /// </summary>
    private string? Password { get; set; }

    /// <summary>
    ///     Get or set if password is embed in note uri.
    /// </summary>
    private bool IsEmbedPasswordMode { get; set; }

    /// <summary>
    ///     Get or set if password is visible.
    /// </summary>
    private bool IsPasswordVisible { get; set; } = true;

    /// <summary>
    ///     Get or set if password is randomly generated.
    /// </summary>
    private bool IsRandomPassword { get; set; } = true;

    /// <summary>
    ///     Get or set password input type for visibility management.
    /// </summary>
    private InputType PasswordInput { get; set; } = InputType.Text;

    /// <summary>
    ///     Get or set password input adornment.
    /// </summary>
    private string PasswordInputIcon { get; set; } = Icons.Material.Filled.Visibility;

    #endregion


    #endregion

    #region Methods

    protected override async Task OnParametersSetAsync()
    {
        if (NoteId == null)
        {
            GenerateSecurePassword();
        }
        else
        {
            Uri uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("pwd", out var pwd))
            {
                IsEmbedPasswordMode = true;
                Password = pwd.First();
                await ReadNote();
            }
            else
            {
                Password = null;
            }
            HidePassword();
        }
    }

    private async Task SendNote()
    {
        if (string.IsNullOrWhiteSpace(Message) == false && string.IsNullOrWhiteSpace(Password) == false)
        {
            try
            {
                if (IsEmbedPasswordMode)
                {
                    GenerateSecurePassword();
                }
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
                NoteLink = (new Uri(new Uri(NavigationManager.Uri), noteId.ToString())).ToString() + (IsEmbedPasswordMode ? $"?pwd={Password}" : "");

                Snackbar.Add("Note sent, copy note link and share it!", MudBlazor.Severity.Success);
            }
            catch (Exception)
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
            catch (Exception)
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

    #region Password management

    public void GenerateSecurePassword(int length = 16)
    {
        string chars = "abcdefghijklmnopqrstuvwxyz0123456789-*:!_";
        char[] password = new char[length];
        byte[] buffer = new byte[length * 8];
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();

        rng.GetBytes(buffer);

        for (int i = 0; i < length; i++)
        {
            uint val = BitConverter.ToUInt32(buffer, i * 8);
            password[i] = chars[(int)(val % (uint)chars.Length)];
        }
        Password = new string(password);
        IsRandomPassword = true;
        ShowPasword();
    }

    private void HidePassword()
    {
        IsPasswordVisible = false;
        PasswordInputIcon = Icons.Material.Filled.VisibilityOff;
        PasswordInput = InputType.Password;
    }

    private void ShowPasword()
    {
        IsPasswordVisible = true;
        PasswordInputIcon = Icons.Material.Filled.Visibility;
        PasswordInput = InputType.Text;
    }

    private void ShowHidePassword()
    {
        if (IsPasswordVisible)
        {
            HidePassword();
        }
        else
        {
            ShowPasword();
        }
    }

    private void HideOnCustomPassword()
    {
        if (IsRandomPassword)
        {
            IsRandomPassword = false;
            HidePassword();
        }
    }

    private void AfterEmbededPassword()
    {
        if (string.IsNullOrWhiteSpace(Password) == true)
        {
            GenerateSecurePassword();
        }
    }

    #endregion

    #endregion
}
