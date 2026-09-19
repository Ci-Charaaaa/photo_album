namespace PhotoAlbum.ViewModels;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PhotoAlbum.Domain.Entities;

//相册树的一个节点
public partial class AlbumNodeViewModel : ObservableObject
{
    //节点对应的相册id
    public long Id { get; }

    //父相册id（根节点为null）
    public long? ParentId { get; }

    //是否是系统内置的未分类相册
    public bool IsUnclassified { get; set; }

    //相册名（可观察，改名后界面自动刷新）
    [ObservableProperty]
    private string _name;

    //是否展开
    [ObservableProperty]
    private bool _isExpanded;

    //是否被选中
    [ObservableProperty]
    private bool _isSelected;

    //子相册集合
    public ObservableCollection<AlbumNodeViewModel> Children { get; } = new();

    //构造函数，从相册实体构建节点
    public AlbumNodeViewModel(Album album)
    {
        Id = album.Id;
        ParentId = album.ParentId;
        _name = album.Name;
    }
}
