using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LimpiadorImagenes.Converters;

[ValueConversion(typeof(object), typeof(Visibility))]
public class NullToVisibilityConverter : IValueConverter
{
    public static readonly NullToVisibilityConverter Instance = new();

    /// <summary>Set to true to invert: null = Visible, non-null = Collapsed.</summary>
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value is null || (value is string s && string.IsNullOrWhiteSpace(s));
        bool visible = Invert ? isNull : !isNull;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
