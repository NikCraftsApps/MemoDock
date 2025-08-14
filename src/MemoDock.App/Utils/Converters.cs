using System;
using System.Globalization;
using System.Windows.Data;

namespace MemoDock.App.Utils
{
    public sealed class BytesToMbConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bytes = System.Convert.ToInt64(value);
            var mb = Math.Max(1, (int)Math.Round(bytes / (1024.0 * 1024.0)));
            return mb;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (int.TryParse(value?.ToString(), out var mb) && mb > 0)
                return (long)mb * 1024L * 1024L;
            return 3 * 1024L * 1024L;
        }
    }
}
