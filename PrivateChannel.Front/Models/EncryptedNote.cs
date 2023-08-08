namespace PrivateChannel.Front.Models;

public class EncryptedNote
{
    #region Properties

    public List<byte> CipherText { get; set; } = new List<byte>();
    public List<byte> AuthTag { get; set; } = new List<byte>();
    public List<byte> IV { get; set; } = new List<byte>();
    public List<byte> Salt { get; set; } = new List<byte>();

    #endregion
}