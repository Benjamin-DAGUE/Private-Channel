namespace PrivateChannel.Back.Core;

public class Peer
{
    #region Properties

    public Guid Id { get; private set; } = Guid.NewGuid();

    public byte[] PublicKey { get; private set; }

    public Action<ConnectToChannelResponse> ConnectionCallback { get; private set; }

    public Action<GetMessagesResponse>? GetMessagesCallback { get; set; }

    #endregion

    #region Constructors

    public Peer(byte[] publicKey, Action<ConnectToChannelResponse> connectionCallback)
    {
        PublicKey = publicKey;
        ConnectionCallback = connectionCallback;
    }

    #endregion
}
