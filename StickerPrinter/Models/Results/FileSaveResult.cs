namespace StickerPrinter.Models.Results;

/// <summary>
/// Модель результата сохранения файла.
/// </summary>
public sealed record FileSaveResult
{
    /// <summary>
    /// Возвращает результат отмененной операции сохранения.
    /// </summary>
    /// <returns>Результат отмененной операции сохранения.</returns>
    public static FileSaveResult Cancelled() => new(false, true, null);

    /// <summary>
    /// Возвращает успешный результат сохранения.
    /// </summary>
    /// <returns>Успешный результат сохранения.</returns>
    public static FileSaveResult Success() => new(true, false, null);

    /// <summary>
    /// Возвращает неуспешный результат сохранения.
    /// </summary>
    /// <param name="errorMessage">Сообщение об ошибке для пользователя.</param>
    /// <returns>Неуспешный результат сохранения.</returns>
    public static FileSaveResult Failure(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new FileSaveResult(false, false, errorMessage);
    }

    private FileSaveResult(bool isSuccess, bool isCancelled, string? errorMessage)
    {
        IsSuccess = isSuccess;
        IsCancelled = isCancelled;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Признак успешного сохранения файла.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Признак отмены сохранения пользователем.
    /// </summary>
    public bool IsCancelled { get; }

    /// <summary>
    /// Сообщение об ошибке для пользователя.
    /// </summary>
    public string? ErrorMessage { get; }
}
