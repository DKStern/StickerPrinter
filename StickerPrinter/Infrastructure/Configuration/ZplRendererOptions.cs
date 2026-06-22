namespace StickerPrinter.Infrastructure.Configuration;

/// <summary>
/// Конфигурация подключения к стороннему сервису рендеринга ZPL.
/// </summary>
public sealed class ZplRendererOptions
{
    /// <summary>
    /// Базовый URL стороннего сервиса рендеринга.
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Относительный путь endpoint рендеринга.
    /// </summary>
    public string RenderEndpoint { get; init; } = string.Empty;

    /// <summary>
    /// Timeout HTTP-запроса в секундах.
    /// </summary>
    public int TimeoutSeconds { get; init; }
}
