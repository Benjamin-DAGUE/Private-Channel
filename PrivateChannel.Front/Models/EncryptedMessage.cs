namespace PrivateChannel.Front.Models;

public class EncryptedMessage
{
    #region Properties

    public List<byte> IV { get; set; } = new List<byte>();
    public List<byte> EncryptedData { get; set; } = new List<byte>();

    #endregion
}