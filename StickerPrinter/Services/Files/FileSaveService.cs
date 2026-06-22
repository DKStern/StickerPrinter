using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using StickerPrinter.Models.Results;

namespace StickerPrinter.Services.Files;

/// <summary>
/// Сервис сохранения PNG-файлов через стандартный диалог сохранения Windows.
/// </summary>
public sealed class FileSaveService : IFileSaveService
{
    private readonly ILogger<FileSaveService> _logger;

    /// <summary>
    /// Создает экземпляр сервиса сохранения файлов.
    /// </summary>
    /// <param name="logger">Логгер сервиса.</param>
    public FileSaveService(ILogger<FileSaveService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<FileSaveResult> SavePngAsync(
        byte[] pngBytes,
        string suggestedFileName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pngBytes);
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestedFileName);

        var dialog = new SaveFileDialog
        {
            FileName = suggestedFileName,
            DefaultExt = ".png",
            Filter = "PNG image (*.png)|*.png",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true)
        {
            return FileSaveResult.Cancelled();
        }

        try
        {
            await File.WriteAllBytesAsync(dialog.FileName, pngBytes, cancellationToken);
            _logger.LogInformation("PNG-файл сохранен. SizeBytes={SizeBytes}", pngBytes.Length);
            return FileSaveResult.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Ошибка сохранения PNG-файла");
            return FileSaveResult.Failure("Не удалось сохранить PNG-файл.");
        }
    }
}
