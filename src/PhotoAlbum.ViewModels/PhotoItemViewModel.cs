namespace PhotoAlbum.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

//照片网格里的一项
public partial class PhotoItemViewModel : ObservableObject
{
    //照片id
    public long Id { get; }

    //显示名
    [ObservableProperty]
    private string _name;

    //备注
    [ObservableProperty]
    private string? _remark;

    //缩略图字节（VM保持UI无关，由View负责转成图像）
    [ObservableProperty]
    private byte[]? _thumbnail;

    //构造函数
    public PhotoItemViewModel(long id, string name)
    {
        Id = id;
        _name = name;
    }
}
