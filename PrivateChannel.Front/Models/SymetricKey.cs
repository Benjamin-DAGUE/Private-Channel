namespace PrivateChannel.Front.Models;

public class SymetricKey
{
    #region Properties

    public List<byte> Raw { get; set; } = new List<byte>();
    public List<byte> Encrypted { get; set; } = new List<byte>();

    #endregion
}
