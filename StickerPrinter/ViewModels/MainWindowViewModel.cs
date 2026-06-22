using Microsoft.Extensions.Logging;
using StickerPrinter.Commands;
using StickerPrinter.Models;
using StickerPrinter.Services.Files;
using StickerPrinter.Services.Messages;
using StickerPrinter.Services.Rendering;
using StickerPrinter.Validation;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace StickerPrinter.ViewModels;

/// <summary>
/// ViewModel главного окна приложения.
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IZplRenderService _zplRenderService;
    private readonly IFileSaveService _fileSaveService;
    private readonly IUserMessageService _userMessageService;
    private readonly ZplInputValidator _zplInputValidator;
    private readonly LabelRenderSettingsValidator _settingsValidator;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly AsyncRelayCommand _renderCommand;
    private readonly RelayCommand _downloadPngCommand;

    private string _zplCode = "^XA\r\n^FO50,50^A0N,40,40^FDHello, ZPL!^FS\r\n^XZ";
    private string _width = "4";
    private string _height = "6";
    private RenderDensity _selectedDensity = RenderDensity.Dpmm8;
    private PreviewState _previewState = PreviewState.Initial;
    private BitmapImage? _previewImage;
    private string _previewMessage = "Предпросмотр появится после создания этикетки.";
    private byte[]? _lastPngBytes;

    /// <summary>
    /// Создает экземпляр ViewModel главного окна.
    /// </summary>
    /// <param name="zplRenderService">Сервис рендеринга ZPL.</param>
    /// <param name="fileSaveService">Сервис сохранения файлов.</param>
    /// <param name="userMessageService">Сервис сообщений пользователю.</param>
    /// <param name="zplInputValidator">Валидатор ZPL-кода.</param>
    /// <param name="settingsValidator">Валидатор параметров этикетки.</param>
    /// <param name="logger">Логгер ViewModel.</param>
    public MainWindowViewModel(
        IZplRenderService zplRenderService,
        IFileSaveService fileSaveService,
        IUserMessageService userMessageService,
        ZplInputValidator zplInputValidator,
        LabelRenderSettingsValidator settingsValidator,
        ILogger<MainWindowViewModel> logger)
    {
        _zplRenderService = zplRenderService ?? throw new ArgumentNullException(nameof(zplRenderService));
        _fileSaveService = fileSaveService ?? throw new ArgumentNullException(nameof(fileSaveService));
        _userMessageService = userMessageService ?? throw new ArgumentNullException(nameof(userMessageService));
        _zplInputValidator = zplInputValidator ?? throw new ArgumentNullException(nameof(zplInputValidator));
        _settingsValidator = settingsValidator ?? throw new ArgumentNullException(nameof(settingsValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _renderCommand = new AsyncRelayCommand(RenderAsync, CanRender);
        _downloadPngCommand = new RelayCommand(DownloadPng, CanDownloadPng);
    }

    /// <summary>
    /// ZPL-код этикетки.
    /// </summary>
    public string ZplCode
    {
        get => _zplCode;
        set
        {
            if (SetProperty(ref _zplCode, value))
            {
                RaiseCommandStates();
            }
        }
    }

    /// <summary>
    /// Ширина этикетки в дюймах.
    /// </summary>
    public string Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    /// <summary>
    /// Высота этикетки в дюймах.
    /// </summary>
    public string Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    /// <summary>
    /// Доступные значения плотности печати.
    /// </summary>
    public IReadOnlyList<RenderDensity> DensityOptions { get; } =
    [
        RenderDensity.Dpmm8,
        RenderDensity.Dpmm12,
        RenderDensity.Dpmm24
    ];

    /// <summary>
    /// Выбранная плотность печати.
    /// </summary>
    public RenderDensity SelectedDensity
    {
        get => _selectedDensity;
        set => SetProperty(ref _selectedDensity, value);
    }

    /// <summary>
    /// Текущее состояние предпросмотра.
    /// </summary>
    public PreviewState PreviewState
    {
        get => _previewState;
        private set
        {
            if (SetProperty(ref _previewState, value))
            {
                OnPropertyChanged(nameof(IsPreviewMessageVisible));
                OnPropertyChanged(nameof(IsPreviewImageVisible));
                RaiseCommandStates();
            }
        }
    }

    /// <summary>
    /// Изображение предпросмотра этикетки.
    /// </summary>
    public BitmapImage? PreviewImage
    {
        get => _previewImage;
        private set => SetProperty(ref _previewImage, value);
    }

    /// <summary>
    /// Сообщение в области предпросмотра.
    /// </summary>
    public string PreviewMessage
    {
        get => _previewMessage;
        private set => SetProperty(ref _previewMessage, value);
    }

    /// <summary>
    /// Признак видимости текстового сообщения предпросмотра.
    /// </summary>
    public bool IsPreviewMessageVisible => PreviewState is not PreviewState.Rendered;

    /// <summary>
    /// Признак видимости изображения предпросмотра.
    /// </summary>
    public bool IsPreviewImageVisible => PreviewState is PreviewState.Rendered && PreviewImage is not null;

    /// <summary>
    /// Команда рендеринга этикетки.
    /// </summary>
    public ICommand RenderCommand => _renderCommand;

    /// <summary>
    /// Команда сохранения последнего PNG-изображения.
    /// </summary>
    public ICommand DownloadPngCommand => _downloadPngCommand;

    private bool CanRender() =>
        PreviewState is not PreviewState.Rendering &&
        !string.IsNullOrWhiteSpace(ZplCode);

    private bool CanDownloadPng() =>
        PreviewState is PreviewState.Rendered &&
        _lastPngBytes is { Length: > 0 };

    private async Task RenderAsync(CancellationToken cancellationToken)
    {
        try
        {
            var zplError = _zplInputValidator.Validate(ZplCode);
            if (zplError is not null)
            {
                SetError(zplError);
                return;
            }

            if (!TryCreateSettings(out var settings, out var settingsError))
            {
                SetError(settingsError);
                return;
            }

            var validationError = _settingsValidator.Validate(settings);
            if (validationError is not null)
            {
                SetError(validationError);
                return;
            }

            PreviewImage = null;
            _lastPngBytes = null;
            PreviewMessage = "Создание предпросмотра...";
            PreviewState = PreviewState.Rendering;

            var result = await _zplRenderService.RenderAsync(ZplCode, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                SetError(result.ErrorMessage ?? "Не удалось создать предпросмотр этикетки.");
                return;
            }

            _lastPngBytes = result.PngBytes;
            PreviewImage = CreateBitmapImage(result.PngBytes);
            PreviewMessage = string.Empty;
            PreviewState = PreviewState.Rendered;
            RaiseCommandStates();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Непредвиденная ошибка ViewModel при создании предпросмотра");
            SetError("Во время создания предпросмотра произошла непредвиденная ошибка.");
        }
    }

    private async void DownloadPng()
    {
        if (_lastPngBytes is null)
        {
            return;
        }

        var result = await _fileSaveService.SavePngAsync(_lastPngBytes, "label.png", CancellationToken.None);
        if (result.IsCancelled)
        {
            return;
        }

        if (result.IsSuccess)
        {
            _userMessageService.ShowInformation("PNG-файл сохранен.");
            return;
        }

        _userMessageService.ShowError(result.ErrorMessage ?? "Не удалось сохранить PNG-файл.");
    }

    private bool TryCreateSettings(out LabelRenderSettings settings, out string errorMessage)
    {
        settings = new LabelRenderSettings(0, 0, SelectedDensity);
        errorMessage = string.Empty;

        if (!decimal.TryParse(Width, NumberStyles.Number, CultureInfo.CurrentCulture, out var width))
        {
            errorMessage = "Введите корректную ширину этикетки.";
            return false;
        }

        if (!decimal.TryParse(Height, NumberStyles.Number, CultureInfo.CurrentCulture, out var height))
        {
            errorMessage = "Введите корректную высоту этикетки.";
            return false;
        }

        settings = new LabelRenderSettings(width, height, SelectedDensity);
        return true;
    }

    private void SetError(string message)
    {
        PreviewImage = null;
        PreviewMessage = message;
        PreviewState = PreviewState.Error;
        _lastPngBytes = null;
        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        _renderCommand.RaiseCanExecuteChanged();
        _downloadPngCommand.RaiseCanExecuteChanged();
    }

    private static BitmapImage CreateBitmapImage(byte[] pngBytes)
    {
        using var stream = new MemoryStream(pngBytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();

        return image;
    }
}
