namespace PrivateChannel.Back.Core;

public class Note
{
    #region Properties

    /// <summary>
    ///     Get or set note Id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Get or set note message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     Get or set note expiration date and time.
    /// </summary>
    public DateTime ExpirationDateTime { get; set; } = DateTime.Now;

    #endregion
}