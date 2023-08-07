namespace PrivateChannel.Front.Models;

public class AsymetricKey
{
    #region Properties

    public List<byte> PublicKey { get; set; } = new List<byte>();
    public List<byte> PrivateKey { get; set; } = new List<byte>();

    #endregion
}