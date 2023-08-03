namespace PrivateChannel.Back.Core;

public class Channel
{
    #region Properties

    public object Locker { get; private set; } = new object();

    public Guid Id { get; private set; }

    public Peer? Peer1 { get; set; }

    public Peer? Peer2 { get; set; }

    #endregion

    #region Constructors

    public Channel(Guid id)
    {
        Id = id;
    }

    #endregion
}
