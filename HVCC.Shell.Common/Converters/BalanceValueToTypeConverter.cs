namespace HVCC.Shell.Common.Converters
{
    using System;
    using System.Windows.Data;

    public class BalanceValueToTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (null != value)
            {
                if ((decimal)value > 0)
                {
                    return "Positive";
                }
                else if ((decimal)value < 0)
                {
                    return "Negative";
                }
                else
                {
                    return "Zero";
                }
            }

            return "Zero";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
