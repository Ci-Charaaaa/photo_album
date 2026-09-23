namespace PhotoAlbum.Presentation.Services;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PhotoAlbum.ViewModels.Services;

//IImageViewerService 的 Avalonia 实现：弹一个独立窗口显示原图。
public class AvaloniaImageViewerService : IImageViewerService
{
    private readonly Window _owner;

    public AvaloniaImageViewerService(Window owner)
    {
        _owner = owner;
    }

    public async Task ViewImageAsync(string title, byte[] imageData)
    {
        //把字节流转成 Avalonia 位图（注意：Bitmap 会持有流，所以这里流用完即可，位图自己保留数据）
        using var stream = new MemoryStream(imageData);
        var bitmap = new Bitmap(stream);

        //Stretch=Uniform：等比缩放填满窗口，不拉伸变形
        var image = new Image
        {
            Source = bitmap,
            Stretch = Stretch.Uniform
        };

        var window = new Window
        {
            Title = title,
            Width = 1000,
            Height = 750,
            Content = image,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        //Bitmap 是非托管资源，窗口关闭时释放，避免内存泄漏
        window.Closed += (_, _) => bitmap.Dispose();

        //ShowDialog 会 await 到窗口关闭（模态）
        await window.ShowDialog(_owner);
    }
}
