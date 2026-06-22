namespace StickerPrinter.Models;

/// <summary>
/// Модель состояния области предпросмотра этикетки.
/// </summary>
public enum PreviewState
{
    /// <summary>
    /// Предпросмотр еще не выполнялся.
    /// </summary>
    Initial,

    /// <summary>
    /// Выполняется рендеринг этикетки.
    /// </summary>
    Rendering,

    /// <summary>
    /// Этикетка успешно отрендерена.
    /// </summary>
    Rendered,

    /// <summary>
    /// При рендеринге произошла ошибка.
    /// </summary>
    Error
}
