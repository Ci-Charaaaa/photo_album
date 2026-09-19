namespace PhotoAlbum.ViewModels;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

//"全部相册"画廊/子相册条里的一张相册卡片
public partial class AlbumCardViewModel : ObservableObject
{
    //相册id
    public long AlbumId { get; }

    //相册名
    [ObservableProperty]
    private string _name;

    //封面缩略图字节（取该相册子树里第一张照片的缩略图）
    [ObservableProperty]
    private byte[]? _thumbnail;

    //点击卡片时执行的命令（由MainViewModel注入）
    public ICommand? OpenCommand { get; set; }

    //构造函数
    public AlbumCardViewModel(long albumId, string name)
    {
        AlbumId = albumId;
        _name = name;
    }
}
