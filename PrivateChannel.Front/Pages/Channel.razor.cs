using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PrivateChannel.Front.Models;
using System.Security.Cryptography.X509Certificates;

namespace PrivateChannel.Front.Pages;

public partial class Channel
{
    #region Fields

    private Guid? _CurrentChannel = null;

    #endregion

    #region Properties

    [Parameter]
    public Guid? ChannelId { get; set; }

    private Guid? PeerId { get; set; }

    private byte[]? PeerPublicKey { get; set; }
    private List<byte>? PeerSessionKey { get; set; }

    public string ChannelLink { get; set; } = String.Empty;

    private List<MessageBase> Messages { get; set; } = new List<MessageBase>();

    private SymetricKey? SymetricKey { get; set; }

    private AsymetricKey? AsymetricKey { get; set; }

    private byte[]? SessionKey { get; set; }

    private bool SessionKeySended { get; set; }

    private int SessionKeyRemainingUsage { get; set; }

    private string TextToSend { get; set; } = string.Empty;

    #endregion

    #region Methods

    protected override async Task OnParametersSetAsync()
    {
        if (ChannelId == null)
        {
            try
            {
                CreateChannelResponse reply = await Client.CreateChannelAsync(new CreateChannelRequest());
                ChannelId = Guid.Parse(reply.ChannelId);
                NavigationManager.NavigateTo($"/channel/{ChannelId}");

            }
            catch (Exception ex)
            {
                throw;
            }
        }
        else if (_CurrentChannel != ChannelId)
        {
            _CurrentChannel = ChannelId;
            ChannelLink = NavigationManager.Uri.ToString();

            AsymetricKey = await JsInterop.InvokeAsync<AsymetricKey>("generateKeyPair");


            _ = Task.Run(async () =>
            {
                try
                {
                    using var call = Client.ConnectToChannel(new ConnectToChannelRequest()
                    {
                        ChannelId = ChannelId.ToString(),
                        PublicKey = ByteString.CopyFrom(AsymetricKey.PublicKey.ToArray())
                    });

                    await foreach (ConnectToChannelResponse connectToChannelResponse in call.ResponseStream.ReadAllAsync())
                    {
                        if (connectToChannelResponse.IsSelf)
                        {
                            PeerId = connectToChannelResponse.IsConnected ? Guid.Parse(connectToChannelResponse.PeerId) : null;

                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    using var call = Client.GetMessages(new GetMessagesRequest()
                                    {
                                        ChannelId = ChannelId.ToString(),
                                        PeerId = PeerId.ToString()
                                    });

                                    await foreach (GetMessagesResponse getMessageResponse in call.ResponseStream.ReadAllAsync())
                                    {
                                        if (PeerSessionKey == null || getMessageResponse.SessionKey.ToByteArray().Length > 0)
                                        {
                                            PeerSessionKey = getMessageResponse.SessionKey.ToByteArray().ToList();
                                        }

                                        string message = await JsInterop.InvokeAsync<string>("decryptWithPrivateKeyAndSymmetricKey",
                                            getMessageResponse.EncryptedMessage.ToByteArray().ToList()
                                            , getMessageResponse.IV.ToByteArray().ToList()
                                            , PeerSessionKey
                                            , AsymetricKey.PrivateKey);

                                        Messages.Add(new TextMessage()
                                        {
                                            Message = message
                                        });
                                        await InvokeAsync(StateHasChanged);
                                    }
                                }
                                catch (Exception ex)
                                {

                                    throw;
                                }
                            });
                        }
                        else
                        {
                            if (connectToChannelResponse.IsConnected)
                            {
                                PeerPublicKey = connectToChannelResponse.PublicKey.ToByteArray();
                                await GenerateSessionKey();
                            }
                            else
                            {
                                PeerPublicKey = null;
                                SymetricKey = null;
                                SessionKey = null;
                                SessionKeySended = false;
                            }
                        }

                        await InvokeAsync(StateHasChanged);
                    }
                }
                catch (Exception)
                {

                    throw;
                }
            });
        }
    }

    private async Task GenerateSessionKey()
    {
        SymetricKey = await JsInterop.InvokeAsync<SymetricKey>("generateAndEncryptSymmetricKey", PeerPublicKey.ToList());
        SessionKey = SymetricKey.Encrypted.ToArray();
        SessionKeyRemainingUsage = 20;
        SessionKeySended = false;
    }

    private async Task CopyChannelLinkToClipboard()
    {
        await JsInterop.InvokeVoidAsync("navigator.clipboard.writeText", ChannelLink);
        Snackbar.Add(Localizer["SnackbarLinkCopied"]);
    }

    private async Task SendMessage()
    {
        if (SymetricKey == null || SessionKeyRemainingUsage <= 0)
        {
            await GenerateSessionKey();
        }

        if (SymetricKey != null && SessionKeyRemainingUsage > 0)
        {
            string messageToSend = TextToSend;

            EncryptedMessage encryptedMessage = await JsInterop.InvokeAsync<EncryptedMessage>("encryptWithSymmetricKey", messageToSend, SymetricKey.Raw);

            await Client. SendMessageAsync(new SendMessageRequest()
            {
                ChannelId = ChannelId.ToString(),
                PeerId = PeerId.ToString(),
                SessionKey = ByteString.CopyFrom(SessionKeySended ? new byte[0] : SessionKey),
                EncryptedMessage = ByteString.CopyFrom(encryptedMessage.EncryptedData.ToArray()),
                IV = ByteString.CopyFrom(encryptedMessage.IV.ToArray())
            });

            SessionKeySended = true;

            Messages.Add(new TextMessage()
            {
                Message = messageToSend,
                IsSender = true
            });

            TextToSend = string.Empty;

            SessionKeyRemainingUsage--;
        }
    }

    #endregion
}
