using System.Windows;

namespace StickerPrinter.Services.Messages;

/// <summary>
/// Сервис показа сообщений пользователю через стандартные диалоги Windows.
/// </summary>
public sealed class UserMessageService : IUserMessageService
{
    /// <inheritdoc />
    public void ShowInformation(string message)
    {
        MessageBox.Show(message, "StickerPrinter", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <inheritdoc />
    public void ShowError(string message)
    {
        MessageBox.Show(message, "StickerPrinter", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
