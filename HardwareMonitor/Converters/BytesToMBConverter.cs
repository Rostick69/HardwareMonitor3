using System;
using System.Globalization;
using System.Windows.Data;

namespace HardwareMonitor.Converters
{
    /// <summary>
    /// Конвертер байтов в мегабайты (для отображения)
    /// </summary>
    public class BytesToMBConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
                return (bytes / 1024.0 / 1024.0).ToString("N0");
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер байтов в гигабайты (для отображения)
    /// </summary>
    public class BytesToGBConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
                return (bytes / 1024.0 / 1024.0 / 1024.0).ToString("N2");
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}