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
    #region Fields

    /// <summary>
    ///     Get or set the current value of <see cref="NoteId"/> parameter to prevent multiple calls in <see cref="OnParametersSetAsync"/>.
    /// </summary>
    private Guid? _CurrentNoteId = Guid.Empty;
    
    #endregion

    #region Properties

    /// <summary>
    ///     Get or site if the component is in loading state.
    /// </summary>
    private bool IsLoading {  get; set; }

    /// <summary>
    ///     Get or set the note identifier. If setted, the component is in read mode.
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

    /// <summary>
    ///     Get or set how many unlock attempts are allowed prior deletion.
    /// </summary>
    private int MaxUnlockAttempts { get; set; } = 5;

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

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (NoteId == null)
        {
            GenerateSecurePassword();
        }
        else if(_CurrentNoteId != NoteId)
        {
            _CurrentNoteId = NoteId;
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
                IsLoading = true;

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
                    MaxUnlockAttempts = MaxUnlockAttempts
                });

                Guid noteId = new Guid(response.Id.ToByteArray());
                NoteLink = (new Uri(new Uri(NavigationManager.Uri), noteId.ToString())).ToString() + (IsEmbedPasswordMode ? $"?pwd={Password}" : "");

                Snackbar.Add(Localizer["SnackbarNoteSent"], MudBlazor.Severity.Success);
            }
            catch (Exception)
            {
                Snackbar.Add(Localizer["SnackbarSendNoteError"], MudBlazor.Severity.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private async Task ReadNote()
    {
        if (NoteId != null && string.IsNullOrWhiteSpace(Message) == true && string.IsNullOrWhiteSpace(Password) == false)
        {
            try
            {
                IsLoading = true;

                ReadNoteResponse response = await Client.ReadNoteAsync(new ReadNoteRequest()
                {
                    Id = ByteString.CopyFrom(NoteId.Value.ToByteArray()),
                    Password = Password
                });

                Message = response.Message;
                Snackbar.Add(Localizer["SnackbarNoteReaded"], MudBlazor.Severity.Success);
            }
            catch (Exception)
            {
                Snackbar.Add(Localizer["SnackbarReadNoteError"], MudBlazor.Severity.Error);
            }
            finally 
            { 
                IsLoading = false; 
            }

            Password = null;
        }
    }

    private async Task CopyNoteToClipboard()
    {
        await JsInterop.InvokeVoidAsync("navigator.clipboard.writeText", Message);
        Snackbar.Add(Localizer["SnackbarCopyNoteMessage"]);
    }

    private async Task CopyNoteLinkToClipboard()
    {
        await JsInterop.InvokeVoidAsync("navigator.clipboard.writeText", NoteLink);
        Snackbar.Add(Localizer["SnackbarCopyNoteLink"]);
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
