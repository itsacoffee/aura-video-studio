using Microsoft.UI.Xaml.Data;
using System;

namespace Aura.App.Converters
{
    /// <summary>
    /// Converter that formats a value using string.Format with the ConverterParameter as the format string.
    /// </summary>
    public class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return string.Empty;

            var format = parameter as string;
            if (string.IsNullOrEmpty(format))
                return value.ToString() ?? string.Empty;

            try
            {
                return string.Format(format, value);
            }
            catch
            {
                return value.ToString() ?? string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // ConvertBack not supported for string formatting - this is a one-way converter
            return value;
        }
    }
}
