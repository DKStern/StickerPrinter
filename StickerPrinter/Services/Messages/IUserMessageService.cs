namespace StickerPrinter.Services.Messages;

/// <summary>
/// Интерфейс сервиса показа сообщений пользователю.
/// </summary>
public interface IUserMessageService
{
    /// <summary>
    /// Показывает информационное сообщение.
    /// </summary>
    /// <param name="message">Текст сообщения.</param>
    void ShowInformation(string message);

    /// <summary>
    /// Показывает сообщение об ошибке.
    /// </summary>
    /// <param name="message">Текст сообщения.</param>
    void ShowError(string message);
}
