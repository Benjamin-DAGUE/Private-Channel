namespace PrivateChannel.DataModel;

public class Note
{
    #region Properties

    /// <summary>
    ///     Get or set note Id.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    ///     Get or set cipher text.
    /// </summary>
    public required byte[] CipherText { get; set; }

    /// <summary>
    ///     Get or set auth tag.
    /// </summary>
    public required byte[] AuthTag { get; set; }

    /// <summary>
    ///     Get or set IV.
    /// </summary>
    public required byte[] IV { get; set; }

    /// <summary>
    ///     Get or set key derivation salt.
    /// </summary>
    public required byte[] Salt { get; set; }

    /// <summary>
    ///     Get or set note expiration date and time.
    /// </summary>
    public required DateTime ExpirationDateTime { get; set; }

    /// <summary>
    ///     Get or set the remaining unlock attempts prior deletion.
    /// </summary>
    public int RemainingUnlockAttempts { get; set; }

    #endregion
}