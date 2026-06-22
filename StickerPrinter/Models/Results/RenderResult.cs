namespace StickerPrinter.Models.Results;

/// <summary>
/// Модель результата рендеринга этикетки.
/// </summary>
public sealed record RenderResult
{
    /// <summary>
    /// Возвращает успешный результат рендеринга.
    /// </summary>
    /// <param name="pngBytes">PNG-изображение этикетки.</param>
    /// <returns>Успешный результат рендеринга.</returns>
    public static RenderResult Success(byte[] pngBytes)
    {
        ArgumentNullException.ThrowIfNull(pngBytes);

        return new RenderResult(true, pngBytes, null);
    }

    /// <summary>
    /// Возвращает неуспешный результат рендеринга.
    /// </summary>
    /// <param name="errorMessage">Сообщение об ошибке для пользователя.</param>
    /// <returns>Неуспешный результат рендеринга.</returns>
    public static RenderResult Failure(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new RenderResult(false, Array.Empty<byte>(), errorMessage);
    }

    private RenderResult(bool isSuccess, byte[] pngBytes, string? errorMessage)
    {
        IsSuccess = isSuccess;
        PngBytes = pngBytes;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Признак успешного рендеринга.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// PNG-изображение этикетки.
    /// </summary>
    public byte[] PngBytes { get; }

    /// <summary>
    /// Сообщение об ошибке для пользователя.
    /// </summary>
    public string? ErrorMessage { get; }
}
