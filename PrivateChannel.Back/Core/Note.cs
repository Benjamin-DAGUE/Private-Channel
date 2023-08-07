namespace PrivateChannel.Back.Core;

public class Note
{
    #region Properties

    /// <summary>
    ///     Get or set note Id.
    /// </summary>
    public required Guid Id { get; set; }
    public required byte[] CipherText { get; set; }
    public required byte[] AuthTag { get; set; }
    public required byte[] IV { get; set; }
    public required byte[] Salt { get; set; }

    /// <summary>
    ///     Get or set note expiration date and time.
    /// </summary>
    public required DateTime ExpirationDateTime { get; set; }

    #endregion
}