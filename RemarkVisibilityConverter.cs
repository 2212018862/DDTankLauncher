using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DDTankLauncher
{
    public class RemarkVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string remark && !string.IsNullOrWhiteSpace(remark))
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
