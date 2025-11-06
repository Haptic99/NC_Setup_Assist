// NC_Setup_Assist/Converters/StringEqualsConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace NC_Setup_Assist.Converters
{
    /// <summary>
    /// Vergleicht einen eingehenden String-Wert mit einem ConverterParameter.
    /// Wird für die RadioButtons in der Sidebar verwendet, um IsChecked zu setzen.
    /// </summary>
    public class StringEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Vergleicht den Wert (z.B. ActiveViewName) mit dem Parameter (z.B. "Dashboard")
            return value?.ToString().Equals(parameter?.ToString(), StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Wir binden IsChecked nur in eine Richtung (OneWay)
            return Binding.DoNothing;
        }
    }
}