using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PrivateChannel.Front.Models;
using System.Security.Cryptography.X509Certificates;

namespace PrivateChannel.Front.Pages;

public class AsymetricKey
{
    public List<int> PublicKey { get; set; } = new List<int>();
    public List<int> PrivateKey { get; set; } = new List<int>();
}

public class SymetricKey
{
    public List<int> Raw { get; set; } = new List<int>();
    public List<int> Encrypted { get; set; } = new List<int>();
}

public class EncryptedMessage
{
    public List<int> IV { get; set; } = new List<int>();
    public List<int> EncryptedData { get; set; } = new List<int>();
}

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

    private List<int>? PeerPublicKeyAsIntList { get; set; }

    public string ChannelLink { get; set; } = String.Empty;

    private List<MessageBase> Messages { get; set; } = new List<MessageBase>();

    private SymetricKey? SymetricKey { get; set; }

    private AsymetricKey? AsymetricKey { get; set; }

    private byte[]? SessionKey { get; set; }

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
                        PublicKey = ByteString.CopyFrom(AsymetricKey.PublicKey.Select(i => (byte)i).ToArray())
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
                                        string message = await JsInterop.InvokeAsync<string>("decryptWithPrivateKeyAndSymmetricKey",
                                            getMessageResponse.EncryptedMessage.ToByteArray().Select(b => (int)b).ToList()
                                            , getMessageResponse.IV.ToByteArray().Select(b => (int)b).ToList()
                                            , getMessageResponse.SessionKey.ToByteArray().Select(b => (int)b).ToList()
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
                                PeerPublicKeyAsIntList = PeerPublicKey.Select(b => (int)b).ToList();

                                SymetricKey = await JsInterop.InvokeAsync<SymetricKey>("generateAndEncryptSymmetricKey", PeerPublicKeyAsIntList);
                                SessionKey = SymetricKey.Encrypted.Select(i => (byte)i).ToArray();
                            }
                            else
                            {
                                PeerPublicKey = null;
                                PeerPublicKeyAsIntList = null;
                                SymetricKey = null;
                                SessionKey = null;
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

    private async Task CopyChannelLinkToClipboard()
    {
        await JsInterop.InvokeVoidAsync("navigator.clipboard.writeText", ChannelLink);
        Snackbar.Add("Channel link copied to clipboard !");
    }

    private async Task SendMessage()
    {
        if (SymetricKey != null)
        {
            string messageToSend = TextToSend;

            EncryptedMessage encryptedMessage = await JsInterop.InvokeAsync<EncryptedMessage>("encryptWithSymmetricKey", messageToSend, SymetricKey.Raw);

            await Client. SendMessageAsync(new SendMessageRequest()
            {
                ChannelId = ChannelId.ToString(),
                PeerId = PeerId.ToString(),
                SessionKey = ByteString.CopyFrom(SessionKey),
                EncryptedMessage = ByteString.CopyFrom(encryptedMessage.EncryptedData.Select(i => (byte)i).ToArray()),
                IV = ByteString.CopyFrom(encryptedMessage.IV.Select(i => (byte)i).ToArray())
            });

            Messages.Add(new TextMessage()
            {
                Message = messageToSend,
                IsSender = true
            });

            TextToSend = string.Empty;
        }
    }

    #endregion
}
