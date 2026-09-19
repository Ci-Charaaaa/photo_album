namespace PhotoAlbum.Presentation.Services;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PhotoAlbum.ViewModels.Services;

//用独立Avalonia窗口显示原图
public class AvaloniaImageViewerService : IImageViewerService
{
    private readonly Window _owner;

    public AvaloniaImageViewerService(Window owner)
    {
        _owner = owner;
    }

    public async Task ViewImageAsync(string title, byte[] imageData)
    {
        using var stream = new MemoryStream(imageData);
        var bitmap = new Bitmap(stream);

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

        //关闭时释放位图
        window.Closed += (_, _) => bitmap.Dispose();

        await window.ShowDialog(_owner);
    }
}
