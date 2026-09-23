namespace PhotoAlbum.Presentation.Converters;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

//值转换器：把 VM 里的缩略图"字节数组"转成 Avalonia 的 Bitmap，供 Image 控件显示。
//VM 层不引用 Avalonia，所以它只存字节；这层负责把字节变成界面能画的图像。
//在 XAML 里这样用：Source="{Binding Thumbnail, Converter={StaticResource BytesToBitmap}}"
public class BytesToBitmapConverter : IValueConverter
{
    //正向：byte[] -> Bitmap（绑定目标方向）
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte[] bytes && bytes.Length > 0)
        {
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);   // ImageSharp 输出的字节由 Avalonia 解码
        }
        return null;   // 没有缩略图 -> 不显示（露出背景色）
    }

    //反向：Bitmap -> byte[]。这里不需要，直接抛"不支持"。
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
