using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ModManager.Converters
{
    // this is used to color invalid rows red
    public class BoolToRedBrushConverter : IValueConverter
    {
        public static readonly BoolToRedBrushConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isValid && !isValid)
            {
                // faint red tint, the warning icon in the # column does most of the work
                return SolidColorBrush.Parse("#1FE5484D");
            }

            // forces default theme.
            return Brushes.Transparent;
        }

        // required by IValueConverter
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}