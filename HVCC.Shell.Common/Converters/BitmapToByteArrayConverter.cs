using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace HVCC.Shell.Common.Converters
{
    public class BitmapToByteArrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Binary v)
            {
                if (v == null || v.Length == 0) return null;
                var image = new BitmapImage();
                using (var mem = new MemoryStream(v.ToArray()))
                {
                    mem.Position = 0;
                    image.BeginInit();
                    image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = null;
                    image.StreamSource = mem;
                    image.EndInit();
                }
                image.Freeze();
                return image;
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is BitmapImage v)
            {
                Binary bytes;
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(v));
                using (MemoryStream ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    bytes = ms.ToArray();
                    return bytes;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
