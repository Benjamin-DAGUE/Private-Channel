using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using PrivateChannel.Back;
using PrivateChannel.Back.Core;
using System.Security.Cryptography;

namespace PrivateChannel.Back.Services;
public class PrivateChannelService : PrivateChannelSvc.PrivateChannelSvcBase
{
    #region Fields

    /// <summary>
    ///     Logger service.
    /// </summary>
    private readonly ILogger<PrivateChannelService> _Logger;

    /// <summary>
    ///     Thread locker for <see cref="_Channels"/> access.
    /// </summary>
    private static object _ChannelsLocker = new object();

    /// <summary>
    ///     Current channels.
    /// </summary>
    private static Dictionary<Guid, Channel> _Channels = new Dictionary<Guid, Channel>();

    /// <summary>
    ///     Ban service.
    /// </summary>
    private BanService _BanService;

    #endregion

    #region Constructors

    /// <summary>
    ///     Initialize a new instance of type <see cref="PrivateChannelService"/>.
    /// </summary>
    /// <param name="logger">Logger service.</param>
    /// <param name="banService">Ban service.</param>
    public PrivateChannelService(ILogger<PrivateChannelService> logger, BanService banService)
    {
        _Logger = logger;
        _BanService = banService;
    }

    #endregion

    #region Methods

    private static Channel GetChannelFromId(Guid id)
    {
        Channel? channel = null;
        lock (_ChannelsLocker)
        {
            if (_Channels.ContainsKey(id))
            {
                channel = _Channels[id];
            }
        }

        if (channel == null)
        {
            throw new Exception("Channel not found");
        }

        return channel;
    }

    public override Task<CreateChannelResponse> CreateChannel(CreateChannelRequest request, ServerCallContext context)
    {
        for (int i = 0; i < 10; i++)
        {
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();

            byte[] randomChannelId = new byte[16];
            rng.GetBytes(randomChannelId);

            Guid guid = new Guid(randomChannelId);

            lock (_ChannelsLocker)
            {
                if (_Channels.ContainsKey(guid) == false)
                {
                    Channel channel = new Channel(guid);

                    _Channels.Add(guid, channel);

                    return Task.FromResult(new CreateChannelResponse
                    {
                        ChannelId = new Guid(randomChannelId).ToString()
                    });
                }
            }
        }

        throw new Exception("No empty channel...");
    }

    public override async Task ConnectToChannel(ConnectToChannelRequest request, IServerStreamWriter<ConnectToChannelResponse> responseStream, ServerCallContext context)
    {
        ConnectToChannelResponse result = new ConnectToChannelResponse();
        Guid channelId = Guid.Parse(request.ChannelId);
        Peer? peer = null;
        Channel? channel = GetChannelFromId(channelId);
        bool isChannelEmpty = false;

        lock (channel.Locker)
        {
            if (channel.Peer1 != null && channel.Peer2 != null)
            {
                throw new Exception("Channel already in use");
            }

            peer = new Peer(request.PublicKey.ToByteArray(), async (response) => await responseStream.WriteAsync(response));
            Peer? otherPeer = null;

            if (channel.Peer1 == null)
            {
                channel.Peer1 = peer;
                otherPeer = channel.Peer2;
            }
            else if (channel.Peer2 == null)
            {
                channel.Peer2 = peer;
                otherPeer = channel.Peer1;
            }

            peer.ConnectionCallback.Invoke(new ConnectToChannelResponse()
            {
                PeerId = peer.Id.ToString(),
                IsSelf = true,
                IsConnected = true,
                PublicKey = ByteString.Empty
            });

            if (otherPeer != null)
            {
                peer.ConnectionCallback.Invoke(new ConnectToChannelResponse()
                {
                    PeerId = otherPeer.Id.ToString(),
                    IsSelf = false,
                    IsConnected = true,
                    PublicKey = ByteString.CopyFrom(otherPeer.PublicKey)
                });

                try
                {
                    otherPeer?.ConnectionCallback(new ConnectToChannelResponse()
                    {
                        PeerId = peer.Id.ToString(),
                        IsSelf = false,
                        IsConnected = true,
                        PublicKey = ByteString.CopyFrom(peer.PublicKey)
                    });
                }
                catch (Exception ex)
                {
                    //TODO : log
                    System.Diagnostics.Debugger.Break();
                }
            }
        }

        while (context.CancellationToken.IsCancellationRequested == false)
        {
            await Task.Delay(200);
        }

        lock (channel.Locker)
        {
            Peer? otherPeer = null;

            if (channel.Peer1 == peer)
            {
                channel.Peer1 = null;
                otherPeer = channel.Peer2;
            }
            else if (channel.Peer2 == peer)
            {
                channel.Peer2 = null;
                otherPeer = channel.Peer1;
            }

            try
            {
                otherPeer?.ConnectionCallback(new ConnectToChannelResponse()
                {
                    PeerId = peer.Id.ToString(),
                    IsSelf = false,
                    IsConnected = false,
                    PublicKey = ByteString.CopyFrom(peer.PublicKey)
                });
            }
            catch (Exception ex)
            {
                //TODO : log
                System.Diagnostics.Debugger.Break();
            }

            isChannelEmpty = otherPeer == null;
        }

        if (isChannelEmpty)
        {
            lock (_ChannelsLocker)
            {
                _Channels.Remove(channelId);
            }
        }
    }

    public override async Task GetMessages(GetMessagesRequest request, IServerStreamWriter<GetMessagesResponse> responseStream, ServerCallContext context)
    {
        Guid channelId = Guid.Parse(request.ChannelId);
        Guid peerId = Guid.Parse(request.PeerId);
        Channel? channel = GetChannelFromId(channelId);
        Peer? peer = null;

        lock (channel.Locker)
        {
            if (channel.Peer1?.Id == peerId)
            {
                peer = channel.Peer1;
            }
            else if (channel.Peer2?.Id == peerId)
            {
                peer = channel.Peer2;
            }
            else
            {
                throw new Exception("Unknown peer id");
            }

            peer.GetMessagesCallback = async (response) => await responseStream.WriteAsync(response);
        }

        while (context.CancellationToken.IsCancellationRequested == false)
        {
            await Task.Delay(200);
        }

        return;
    }

    public override async Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
    {
        Guid channelId = Guid.Parse(request.ChannelId);
        Guid peerId = Guid.Parse(request.PeerId);
        Channel? channel = GetChannelFromId(channelId);
        Peer? peer = null;

        while (context.CancellationToken.IsCancellationRequested == false)
        {
            lock (channel.Locker)
            {
                if (channel.Peer1 != null && channel.Peer1.Id != peerId)
                {
                    peer = channel.Peer1;
                }
                else if (channel.Peer2 != null && channel.Peer2.Id != peerId)
                {
                    peer = channel.Peer2;
                }
                else
                {
                    throw new Exception("Unknown peer id");
                }

                if (peer.GetMessagesCallback != null)
                {
                    peer.GetMessagesCallback.Invoke(new GetMessagesResponse()
                    {
                        SessionKey = request.SessionKey,
                        EncryptedMessage = request.EncryptedMessage,
                        IV = request.IV,
                    });
                    break;
                }
            }
            await Task.Delay(200);
        }

        return new SendMessageResponse();
    }

    #endregion
}
