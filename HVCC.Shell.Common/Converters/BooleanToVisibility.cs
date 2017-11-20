namespace HVCC.Shell.Common.Converters
{
    using System;
    using System.Windows.Data;

    /// <summary>
    /// Converts boolean to visibility (true = visible, false = hidden)
    /// </summary>
    public class BooleanToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (true == (Boolean)value)
            {
                return System.Windows.Visibility.Visible;
            }
            else
            {
                return System.Windows.Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            System.Windows.Visibility vis = (System.Windows.Visibility)value;
            switch (vis)
            {
                case System.Windows.Visibility.Visible:
                    return true;
                case System.Windows.Visibility.Hidden:
                    return false;
                default:
                    return false;                
            }
        }
    }


    /// <summary>
    /// Converts boolean to visibility (true = hidden, false = visible)
    /// </summary>

    public class InverseBooleanToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (false == (Boolean)value)
            {
                return System.Windows.Visibility.Visible;
            }
            else
            {
                return System.Windows.Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            System.Windows.Visibility vis = (System.Windows.Visibility)value;
            switch (vis)
            {
                case System.Windows.Visibility.Visible:
                    return false;
                case System.Windows.Visibility.Hidden:
                    return true;
                default:
                    return true;
            }
        }
    }

}
