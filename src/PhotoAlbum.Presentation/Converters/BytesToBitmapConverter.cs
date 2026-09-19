namespace PhotoAlbum.Presentation.Converters;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

//把缩略图字节数组转成Avalonia的Bitmap，供Image控件显示
public class BytesToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte[] bytes && bytes.Length > 0)
        {
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
