namespace PhotoAlbum.ViewModels;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

//"全部相册"画廊/子相册条 里的一张相册卡片（也是视图模型）。
//卡片=封面缩略图+相册名+一个"打开"命令。
public partial class AlbumCardViewModel : ObservableObject
{
    //相册id
    public long AlbumId { get; }

    //相册名（可观察）
    [ObservableProperty]
    private string _name;

    //封面缩略图字节（由MainViewModel递归解析：本相册第一张，或子树里第一张）
    [ObservableProperty]
    private byte[]? _thumbnail;

    //点击卡片时执行的命令
    //为什么把命令放在卡片上、而不是用ListBox的选中项？
    //  用"选中项"触发会导致：打开相册时要清空卡片集合，而清空又会让 ListBox 的
    //  选中索引越界（ArgumentOutOfRange），并且选中被清后"再点无反应"
    //  改成卡片自带命令按钮，就能正常单击即触发
    public ICommand? OpenCommand { get; set; }

    //构造函数
    public AlbumCardViewModel(long albumId, string name)
    {
        AlbumId = albumId;
        _name = name;
    }
}
