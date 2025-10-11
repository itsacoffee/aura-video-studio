using Microsoft.UI.Xaml.Data;
using System;

namespace Aura.App.Converters
{
    /// <summary>
    /// Converter that formats a TimeSpan value into a human-readable string.
    /// </summary>
    public class TimeSpanFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return "N/A";

            if (value is TimeSpan timeSpan)
            {
                if (timeSpan.TotalSeconds < 1)
                    return "< 1s";
                
                if (timeSpan.TotalHours >= 1)
                    return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
                
                if (timeSpan.TotalMinutes >= 1)
                    return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
                
                return $"{timeSpan.Seconds}s";
            }

            return value.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // ConvertBack not supported for TimeSpan formatting - this is a one-way converter
            return TimeSpan.Zero;
        }
    }
}
