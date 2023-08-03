namespace PrivateChannel.Front.Models;

public class MessageBase
{
    #region Properties
    public bool IsSender { get; set; } = false;

    public DateTime DateTime { get; set; } = DateTime.Now;

    #endregion
}
