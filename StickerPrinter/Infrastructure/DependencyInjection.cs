using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using StickerPrinter.Infrastructure.Configuration;
using StickerPrinter.Services.Files;
using StickerPrinter.Services.Messages;
using StickerPrinter.Services.Rendering;
using StickerPrinter.Validation;
using StickerPrinter.ViewModels;

namespace StickerPrinter.Infrastructure;

/// <summary>
/// Инфраструктура регистрации зависимостей приложения.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует сервисы приложения.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    /// <returns>Коллекция сервисов.</returns>
    public static IServiceCollection AddStickerPrinter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<ZplRendererOptions>()
            .Bind(configuration.GetSection("ZplRenderer"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.BaseUrl), "ZplRenderer:BaseUrl обязателен.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.RenderEndpoint), "ZplRenderer:RenderEndpoint обязателен.")
            .Validate(options => options.TimeoutSeconds > 0, "ZplRenderer:TimeoutSeconds должен быть больше нуля.")
            .ValidateOnStart();

        services
            .AddHttpClient(ZplRenderService.HttpClientName, (serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<ZplRendererOptions>>().Value;
                httpClient.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddPolicyHandler((serviceProvider, _) =>
            {
                return Policy<HttpResponseMessage>
                    .Handle<HttpRequestException>()
                    .OrResult(response =>
                        response.StatusCode is HttpStatusCode.RequestTimeout ||
                        (int)response.StatusCode >= 500)
                    .WaitAndRetryAsync(
                        retryCount: 3,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(250 * retryAttempt));
            });

        services.AddSingleton<IZplRenderService, ZplRenderService>();
        services.AddSingleton<IFileSaveService, FileSaveService>();
        services.AddSingleton<IUserMessageService, UserMessageService>();
        services.AddSingleton<ZplInputValidator>();
        services.AddSingleton<LabelRenderSettingsValidator>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}
