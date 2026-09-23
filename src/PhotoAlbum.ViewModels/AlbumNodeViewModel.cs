namespace PhotoAlbum.ViewModels;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PhotoAlbum.Domain.Entities;

//相册树的一个节点（视图模型）
//说明：它不是实体Album，而是"给界面看的一层包装"，所以里面只放界面需要的字段
//从实体构建：Album -> AlbumNodeViewModel（投影）
public partial class AlbumNodeViewModel : ObservableObject
{
    //节点对应的相册id（投影自实体的Id，构建后不再变，所以是只读属性）
    public long Id { get; }

    //父相册id（投影自实体；根节点为null）
    public long? ParentId { get; }

    //是否是系统内置的"未分类"相册（由加载时对比id标记，界面/逻辑据此特判）
    public bool IsUnclassified { get; set; }

    //相册名（[ObservableProperty] 自动生成Name属性：赋值时通知界面刷新）
    //ObservableProperty是一个源生成器特性，它会自动生成一个公开的Name、
    //属性，并在设置值时触发 PropertyChanged 事件，从而通知界面更新
    [ObservableProperty]
    private string _name;

    //相册备注（可观察，改完界面能自动更新）
    [ObservableProperty]
    private string? _remark;

    //是否展开（TreeView展开状态；这里保留字段以备绑定）
    [ObservableProperty]
    private bool _isExpanded;

    //是否被选中（保留字段；实际选中由MainViewModel.SelectedAlbum统一管理）
    [ObservableProperty]
    private bool _isSelected;

    //子相册集合。用 ObservableCollection：增删子节点时 TreeView 自动更新。
    public ObservableCollection<AlbumNodeViewModel> Children { get; } = new();

    //构造函数：把实体的字段搬进来（投影）
    public AlbumNodeViewModel(Album album)
    {
        Id = album.Id;
        ParentId = album.ParentId;
        _name = album.Name;
        _remark = album.Remark;
    }
}
