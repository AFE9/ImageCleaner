using System.Globalization;
using System.Windows.Data;
using LimpiadorImagenes.Models;

namespace LimpiadorImagenes.Converters;

/// <summary>Converts WorkMode enum to bool for ToggleButton IsChecked binding.
/// ConverterParameter should be the string name of the enum value.</summary>
[ValueConversion(typeof(WorkMode), typeof(bool))]
public class EnumToBoolConverter : IValueConverter
{
    public static readonly EnumToBoolConverter ModeConverter = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is WorkMode mode && parameter is string param)
            return Enum.TryParse<WorkMode>(param, out var target) && mode == target;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter is string param)
        {
            if (Enum.TryParse<WorkMode>(param, out var target))
                return target;
        }
        return Binding.DoNothing;
    }
}
