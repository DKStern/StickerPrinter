using System.Globalization;
using System.IO;
using Serilog;
using Serilog.Events;

namespace StickerPrinter.Infrastructure.Logging;

/// <summary>
/// Конфигурация логирования приложения через Serilog.
/// </summary>
public static class SerilogConfiguration
{
    private const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Создает конфигурацию Serilog для приложения.
    /// </summary>
    /// <returns>Настроенный логгер Serilog.</returns>
    public static ILogger CreateLogger()
    {
        var sessionLogDirectory = Path.Combine(
            "logs",
            DateTimeOffset.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture));

        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: Path.Combine(sessionLogDirectory, "application.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: OutputTemplate)
            .WriteTo.File(
                path: Path.Combine(sessionLogDirectory, "errors.log"),
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: OutputTemplate)
            .CreateLogger();
    }
}
