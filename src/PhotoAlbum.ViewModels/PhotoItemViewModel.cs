namespace PhotoAlbum.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

//照片界面里的一张照片的视图模型
//它是"照片实体投影到界面"的结果：只包含界面要显示的字段
//注意：这里存的是缩略图的"字节数组"，不是图片对象，即VM保持与UI框架无关，
//由 View 层的转换器(BytesToBitmapConverter)把字节转成可显示的图像
public partial class PhotoItemViewModel : ObservableObject
{
    //照片id（投影自实体，构建后不变）
    public long Id { get; }

    //显示名（可观察：重命名后界面自动刷新）
    [ObservableProperty]
    private string _name;

    //备注（可观察）
    [ObservableProperty]
    private string? _remark;

    //缩略图字节（可观察：异步加载完成后界面自动显示）
    [ObservableProperty]
    private byte[]? _thumbnail;

    //构造函数
    public PhotoItemViewModel(long id, string name)
    {
        Id = id;
        _name = name;
    }
}
