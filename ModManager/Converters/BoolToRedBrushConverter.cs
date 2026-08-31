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
                // semi-transparent red
                return SolidColorBrush.Parse("#33FF0000");
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