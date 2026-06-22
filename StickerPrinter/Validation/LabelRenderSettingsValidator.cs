using Microsoft.Extensions.Logging;
using StickerPrinter.Models;

namespace StickerPrinter.Validation;

/// <summary>
/// Валидатор параметров рендеринга этикетки.
/// </summary>
public sealed class LabelRenderSettingsValidator
{
    private readonly ILogger<LabelRenderSettingsValidator> _logger;

    /// <summary>
    /// Создает экземпляр валидатора параметров этикетки.
    /// </summary>
    /// <param name="logger">Логгер валидатора.</param>
    public LabelRenderSettingsValidator(ILogger<LabelRenderSettingsValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Выполняет валидацию параметров рендеринга.
    /// </summary>
    /// <param name="settings">Параметры рендеринга.</param>
    /// <returns>Сообщение об ошибке или <see langword="null" />, если параметры корректны.</returns>
    public string? Validate(LabelRenderSettings settings)
    {
        if (settings.Width is < 1 or > 10)
        {
            var message = "Ширина этикетки должна быть от 1 до 10 дюймов.";
            _logger.LogWarning("Ошибка валидации: некорректная ширина этикетки {Width}", settings.Width);
            return message;
        }

        if (settings.Height is < 1 or > 20)
        {
            var message = "Высота этикетки должна быть от 1 до 20 дюймов.";
            _logger.LogWarning("Ошибка валидации: некорректная высота этикетки {Height}", settings.Height);
            return message;
        }

        if (!Enum.IsDefined(settings.Density))
        {
            var message = "Плотность печати должна быть 8, 12 или 24 dpmm.";
            _logger.LogWarning("Ошибка валидации: некорректная плотность печати {Density}", settings.Density);
            return message;
        }

        return null;
    }
}
