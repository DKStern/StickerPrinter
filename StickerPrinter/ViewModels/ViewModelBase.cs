using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StickerPrinter.ViewModels;

/// <summary>
/// ViewModel базового типа с поддержкой уведомлений об изменениях свойств.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Обновляет значение свойства и уведомляет UI об изменении.
    /// </summary>
    /// <typeparam name="T">Тип значения свойства.</typeparam>
    /// <param name="field">Поле, хранящее значение свойства.</param>
    /// <param name="value">Новое значение свойства.</param>
    /// <param name="propertyName">Имя свойства.</param>
    /// <returns><see langword="true" />, если значение было изменено.</returns>
    protected bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);

        return true;
    }

    /// <summary>
    /// Уведомляет UI об изменении свойства.
    /// </summary>
    /// <param name="propertyName">Имя свойства.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
