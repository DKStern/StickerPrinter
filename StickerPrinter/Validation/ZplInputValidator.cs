using Microsoft.Extensions.Logging;
using System.Text;

namespace StickerPrinter.Validation;

/// <summary>
/// Валидатор ZPL-кода.
/// </summary>
public sealed class ZplInputValidator
{
    private const int MaxSizeBytes = 256 * 1024;
    private readonly ILogger<ZplInputValidator> _logger;

    /// <summary>
    /// Создает экземпляр валидатора ZPL-кода.
    /// </summary>
    /// <param name="logger">Логгер валидатора.</param>
    public ZplInputValidator(ILogger<ZplInputValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Выполняет валидацию ZPL-кода.
    /// </summary>
    /// <param name="zpl">ZPL-код.</param>
    /// <returns>Сообщение об ошибке или <see langword="null" />, если значение корректно.</returns>
    public string? Validate(string? zpl)
    {
        if (zpl is null)
        {
            const string message = "Введите ZPL-код.";
            _logger.LogWarning("Ошибка валидации: ZPL-код равен null");
            return message;
        }

        if (zpl.Length == 0 || string.IsNullOrWhiteSpace(zpl))
        {
            const string message = "Введите ZPL-код.";
            _logger.LogWarning("Ошибка валидации: ZPL-код пустой или состоит из пробелов");
            return message;
        }

        var size = Encoding.UTF8.GetByteCount(zpl);
        if (size > MaxSizeBytes)
        {
            const string message = "ZPL-код слишком большой. Максимальный размер - 256 KB.";
            _logger.LogWarning("Ошибка валидации: размер ZPL-кода {ZplSizeBytes} превышает лимит {MaxSizeBytes}", size, MaxSizeBytes);
            return message;
        }

        return null;
    }
}
