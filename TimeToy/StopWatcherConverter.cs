using System;
using System.Globalization;
using System.Windows.Data;

namespace TimeToy
{
    public class StopWatcherConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return 72.0;
            if (values[0] is double width && values[1] is double height)
            {
                if (width < 250 || height < 35)
                    return 24.0;
                if (width < 500 || height < 50)
                    return 36.0;
            }
            return 64.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}