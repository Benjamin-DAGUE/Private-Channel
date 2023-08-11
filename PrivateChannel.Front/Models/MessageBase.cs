namespace PrivateChannel.Front.Models;

public class MessageBase
{
    #region Properties

    public required Guid Id { get; set; }

    public bool IsSender { get; set; } = false;

    public DateTime DateTime { get; set; } = DateTime.Now;

    #endregion
}
