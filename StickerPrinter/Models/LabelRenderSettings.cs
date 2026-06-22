namespace StickerPrinter.Models;

/// <summary>
/// Модель параметров рендеринга этикетки.
/// </summary>
/// <param name="Width">Ширина этикетки в дюймах.</param>
/// <param name="Height">Высота этикетки в дюймах.</param>
/// <param name="Density">Плотность печати этикетки.</param>
public sealed record LabelRenderSettings(
    decimal Width,
    decimal Height,
    RenderDensity Density);
