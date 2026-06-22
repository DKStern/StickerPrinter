using StickerPrinter.ViewModels;
using System.Windows;

namespace StickerPrinter;
/// <summary>
/// Окно главного пользовательского интерфейса приложения.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Создает экземпляр главного окна приложения.
    /// </summary>
    /// <param name="viewModel">ViewModel главного окна.</param>
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}
