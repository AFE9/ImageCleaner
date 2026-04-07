using System.Globalization;
using System.Windows.Data;

namespace LimpiadorImagenes.Converters;

/// <summary>Converts (currentBytes, totalBytes, barTotalWidth) to a pixel width for progress bars.</summary>
public class ProgressBarWidthConverter : IMultiValueConverter
{
    public static readonly ProgressBarWidthConverter Instance = new();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3) return 0.0;
        if (values[0] is not long current) return 0.0;
        if (values[1] is not long total || total <= 0) return 0.0;
        if (values[2] is not double barWidth) return 0.0;

        return Math.Max(0, Math.Min(barWidth, barWidth * current / total));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
