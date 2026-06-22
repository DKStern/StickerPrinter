using StickerPrinter.Models;
using StickerPrinter.Models.Results;

namespace StickerPrinter.Services.Rendering;

/// <summary>
/// Интерфейс сервиса рендеринга ZPL-кода в изображение этикетки.
/// </summary>
public interface IZplRenderService
{
    /// <summary>
    /// Выполняет рендеринг ZPL-кода с указанными параметрами этикетки.
    /// </summary>
    /// <param name="zpl">ZPL-код для рендеринга.</param>
    /// <param name="settings">Параметры этикетки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Результат рендеринга.</returns>
    Task<RenderResult> RenderAsync(
        string zpl,
        LabelRenderSettings settings,
        CancellationToken cancellationToken);
}
