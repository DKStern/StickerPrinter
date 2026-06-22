using StickerPrinter.Models.Results;

namespace StickerPrinter.Services.Files;

/// <summary>
/// Интерфейс сервиса сохранения PNG-файла.
/// </summary>
public interface IFileSaveService
{
    /// <summary>
    /// Сохраняет PNG-изображение на диск.
    /// </summary>
    /// <param name="pngBytes">PNG-изображение.</param>
    /// <param name="suggestedFileName">Предлагаемое имя файла.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Результат сохранения файла.</returns>
    Task<FileSaveResult> SavePngAsync(
        byte[] pngBytes,
        string suggestedFileName,
        CancellationToken cancellationToken);
}
