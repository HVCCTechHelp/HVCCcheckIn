namespace HVCC.Shell.Common.Converters
{
    using System;
    using System.Windows.Data;
    public class InvertBoolean : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (true == (Boolean)value)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (true == (Boolean)value)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
