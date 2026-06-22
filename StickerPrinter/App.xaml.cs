using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using StickerPrinter.Infrastructure;
using StickerPrinter.Infrastructure.Logging;
using System.Windows;
using System.Windows.Threading;

namespace StickerPrinter;
/// <summary>
/// Приложение WPF StickerPrinter.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        Log.Logger = SerilogConfiguration.CreateLogger();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            _host = Host.CreateDefaultBuilder(e.Args)
                .UseSerilog()
                .ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddStickerPrinter(context.Configuration);
                })
                .Build();

            await _host.StartAsync();

            Log.Information("Приложение запущено");

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Ошибка запуска приложения");
            MessageBox.Show(
                "Не удалось запустить приложение. Подробности записаны в лог.",
                "StickerPrinter",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
        }
    }

    /// <inheritdoc />
    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            Log.Information("Приложение завершает работу");

            if (_host is not null)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
                _host.Dispose();
            }
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Ошибка завершения приложения");
        }
        finally
        {
            Log.CloseAndFlush();
        }

        base.OnExit(e);
    }

    /// <summary>
    /// Обрабатывает необработанные исключения UI-потока.
    /// </summary>
    private static void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Необработанное исключение в UI-потоке");
        MessageBox.Show(
            "Произошла непредвиденная ошибка. Подробности записаны в лог.",
            "StickerPrinter",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    /// <summary>
    /// Обрабатывает необработанные исключения домена приложения.
    /// </summary>
    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Log.Fatal(exception, "Необработанное исключение приложения");
        }
    }

    /// <summary>
    /// Обрабатывает ненаблюдаемые исключения фоновых задач.
    /// </summary>
    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Необработанное исключение фоновой задачи");
        e.SetObserved();
    }
}

