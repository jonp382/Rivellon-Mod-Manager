using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ModManager.Converters
{
    // this is used to convert strings to tooltips
    public class StringToTooltipConverter : IValueConverter
    {
        public static readonly StringToTooltipConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            // returning null prevents blank tooltips from showing up.
            return null;
        }

        // required by IValueConverter
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}