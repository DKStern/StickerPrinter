using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StickerPrinter.Infrastructure.Configuration;
using StickerPrinter.Models;
using StickerPrinter.Models.Results;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace StickerPrinter.Services.Rendering;

/// <summary>
/// Сервис рендеринга ZPL-кода через сторонний HTTP API.
/// </summary>
public sealed class ZplRenderService : IZplRenderService
{
    internal const string HttpClientName = "ZplRenderer";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ZplRendererOptions _options;
    private readonly ILogger<ZplRenderService> _logger;

    /// <summary>
    /// Создает экземпляр сервиса рендеринга ZPL.
    /// </summary>
    /// <param name="httpClientFactory">Фабрика HTTP-клиентов для вызова стороннего API.</param>
    /// <param name="options">Настройки стороннего API.</param>
    /// <param name="logger">Логгер сервиса.</param>
    public ZplRenderService(
        IHttpClientFactory httpClientFactory,
        IOptions<ZplRendererOptions> options,
        ILogger<ZplRenderService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ValidateOptions(_options);
    }

    /// <inheritdoc />
    public async Task<RenderResult> RenderAsync(
        string zpl,
        LabelRenderSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zpl);

        var zplLength = zpl.Length;
        var zplHash = GetShortHash(zpl);
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Начат рендеринг этикетки. ZplLength={ZplLength}, ZplHash={ZplHash}, Width={Width}, Height={Height}, Density={Density}",
            zplLength,
            zplHash,
            settings.Width,
            settings.Height,
            (int)settings.Density);

        try
        {
            var request = new RenderRequest(
                zpl,
                settings.Width,
                settings.Height,
                (int)settings.Density);

            var httpClient = _httpClientFactory.CreateClient(HttpClientName);
            using var httpRequest = CreateHttpRequest(request);
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var apiMessage = await TryReadApiErrorMessageAsync(response, cancellationToken);
                var userMessage = CreateUserMessage(response.StatusCode, apiMessage);

                _logger.LogWarning(
                    "Ошибка рендеринга от стороннего API. StatusCode={StatusCode}, DurationMs={DurationMs}, ZplLength={ZplLength}, ZplHash={ZplHash}",
                    (int)response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    zplLength,
                    zplHash);

                return RenderResult.Failure(userMessage);
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (!string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Ошибка рендеринга: неожиданный тип содержимого {ContentType}. DurationMs={DurationMs}",
                    contentType,
                    stopwatch.ElapsedMilliseconds);

                return RenderResult.Failure("Сервис рендеринга вернул неожиданный формат ответа.");
            }

            var pngBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            _logger.LogInformation(
                "Рендеринг этикетки завершен. StatusCode={StatusCode}, DurationMs={DurationMs}, PngSizeBytes={PngSizeBytes}, ZplLength={ZplLength}, ZplHash={ZplHash}",
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                pngBytes.Length,
                zplLength,
                zplHash);

            return RenderResult.Success(pngBytes);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger.LogError(
                exception,
                "Превышено время ожидания при рендеринге этикетки. DurationMs={DurationMs}, TimeoutSeconds={TimeoutSeconds}, ZplLength={ZplLength}, ZplHash={ZplHash}",
                stopwatch.ElapsedMilliseconds,
                _options.TimeoutSeconds,
                zplLength,
                zplHash);

            return RenderResult.Failure("Сервис рендеринга не ответил вовремя. Повторите попытку позже.");
        }
        catch (HttpRequestException exception)
        {
            stopwatch.Stop();

            _logger.LogError(
                exception,
                "HTTP-ошибка при рендеринге этикетки. DurationMs={DurationMs}, ZplLength={ZplLength}, ZplHash={ZplHash}",
                stopwatch.ElapsedMilliseconds,
                zplLength,
                zplHash);

            return RenderResult.Failure("Не удалось подключиться к сервису рендеринга.");
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            _logger.LogError(
                exception,
                "Непредвиденная ошибка рендеринга этикетки. DurationMs={DurationMs}, ZplLength={ZplLength}, ZplHash={ZplHash}",
                stopwatch.ElapsedMilliseconds,
                zplLength,
                zplHash);

            return RenderResult.Failure("Во время рендеринга произошла непредвиденная ошибка.");
        }
    }

    /// <summary>
    /// Проверяет корректность настроек сервиса рендеринга.
    /// </summary>
    private static void ValidateOptions(ZplRendererOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.BaseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.RenderEndpoint);

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("ZplRenderer:BaseUrl должен быть абсолютным URL.");
        }

        if (options.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("ZplRenderer:TimeoutSeconds должен быть больше нуля.");
        }
    }

    /// <summary>
    /// Вычисляет короткий hash значения для безопасного логирования.
    /// </summary>
    private static string GetShortHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes, 0, 8);
    }

    /// <summary>
    /// Создает HTTP-запрос к сервису рендеринга.
    /// </summary>
    private HttpRequestMessage CreateHttpRequest(RenderRequest request)
    {
        var endpoint = BuildEndpoint(request);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(request.Zpl, Encoding.UTF8, "application/x-www-form-urlencoded")
        };

        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png"));

        return httpRequest;
    }

    /// <summary>
    /// Формирует endpoint рендеринга из шаблона и параметров этикетки.
    /// </summary>
    private string BuildEndpoint(RenderRequest request)
    {
        return _options.RenderEndpoint
            .Replace("{density}", request.Density.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{width}", request.Width.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{height}", request.Height.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    /// <summary>
    /// Пытается прочитать сообщение об ошибке из ответа API.
    /// </summary>
    private static async Task<string?> TryReadApiErrorMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            return document.RootElement.TryGetProperty("message", out var message)
                ? message.GetString()
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Создает пользовательское сообщение по HTTP-статусу и ответу API.
    /// </summary>
    private static string CreateUserMessage(HttpStatusCode statusCode, string? apiMessage)
    {
        if (!string.IsNullOrWhiteSpace(apiMessage))
        {
            return apiMessage;
        }

        return statusCode switch
        {
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity =>
                "Сервис рендеринга отклонил ZPL-код или параметры этикетки.",
            >= HttpStatusCode.InternalServerError =>
                "Сервис рендеринга временно недоступен. Повторите попытку позже.",
            _ => "Не удалось выполнить рендеринг этикетки."
        };
    }

    private sealed record RenderRequest(
        string Zpl,
        decimal Width,
        decimal Height,
        int Density);
}
