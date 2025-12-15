using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NC_Setup_Assist.Converters
{
    public class ViewModelTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Visible;

            Type viewModelType = value.GetType();
            Type targetViewModelType = parameter as Type;

            if (targetViewModelType != null && viewModelType == targetViewModelType)
            {
                // If the current ViewModel matches the target type (e.g. MainMenuViewModel),
                // we want to HIDE the sidebar (Collapsed).
                return Visibility.Collapsed;
            }

            // Otherwise show it
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
