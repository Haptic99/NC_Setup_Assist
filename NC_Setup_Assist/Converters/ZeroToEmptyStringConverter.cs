using System;
using System.Globalization;
using System.Windows.Data;

namespace NC_Setup_Assist.Converters
{
    public class ZeroToEmptyStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue && intValue == 0)
            {
                return string.Empty;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && string.IsNullOrEmpty(stringValue))
            {
                return 0;
            }
            return value;
        }
    }
}