namespace PhotoAlbum.ViewModels;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Application.Services;
using PhotoAlbum.Domain.Entities;
using PhotoAlbum.ViewModels.Services;

//主界面视图模型：持有相册树、当前选中相册、照片列表，以及各操作命令
//原则：VM不认识数据库/文件，只通过"开scope -> 取应用服务 -> 调用 -> 释放"来干活
public partial class MainViewModel : ObservableObject
{
    //依赖：作用域工厂 + 两个UI能力接口
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialog;
    private readonly IFilePickerService _filePicker;

    //每屏加载的照片数
    private const int PageSize = 40;

    //构造函数，传入依赖
    public MainViewModel(IServiceScopeFactory scopeFactory, IDialogService dialog, IFilePickerService filePicker)
    {
        _scopeFactory = scopeFactory;
        _dialog = dialog;
        _filePicker = filePicker;
    }

    //相册树（根节点集合）
    public ObservableCollection<AlbumNodeViewModel> AlbumTree { get; } = new();

    //当前选中的相册（null表示"全部照片"视图）
    [ObservableProperty]
    private AlbumNodeViewModel? _selectedAlbum;

    //照片列表
    public ObservableCollection<PhotoItemViewModel> Photos { get; } = new();

    //当前选中的照片
    [ObservableProperty]
    private PhotoItemViewModel? _selectedPhoto;

    //是否忙碌（供界面显示加载状态）
    [ObservableProperty]
    private bool _isBusy;

    //选中相册变化时，自动重新加载照片
    partial void OnSelectedAlbumChanged(AlbumNodeViewModel? value)
    {
        //属性变化回调里不能await，用丢弃方式触发
        _ = LoadPhotosAsync();
    }

    //初始加载：相册树 + 照片
    [RelayCommand]
    private async Task LoadAsync()
    {
        await LoadAlbumTreeAsync();
        await LoadPhotosAsync();
    }

    //显示全部照片（清空选中相册）
    [RelayCommand]
    private async Task ShowAllAsync()
    {
        if (SelectedAlbum == null)
            await LoadPhotosAsync();
        else
            SelectedAlbum = null;   // 触发 OnSelectedAlbumChanged 自动加载
    }

    //新建相册：在当前选中相册下建子相册（未选中则为根相册）
    [RelayCommand]
    private async Task CreateAlbumAsync()
    {
        string? name = await _dialog.PromptAsync("新建相册", "请输入相册名称", string.Empty);
        if (string.IsNullOrWhiteSpace(name))
            return;

        long? parentId = SelectedAlbum?.Id;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var albums = scope.ServiceProvider.GetRequiredService<AlbumService>();
            await albums.CreateAlbumAsync(name.Trim(), parentId);
            await LoadAlbumTreeAsync();
        }
        catch (Exception ex)
        {
            await _dialog.InfoAsync("新建失败", ex.Message);
        }
    }

    //重命名选中的相册
    [RelayCommand]
    private async Task RenameAlbumAsync()
    {
        if (SelectedAlbum == null)
            return;

        string? name = await _dialog.PromptAsync("重命名相册", "请输入新的名称", SelectedAlbum.Name);
        if (string.IsNullOrWhiteSpace(name))
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var albums = scope.ServiceProvider.GetRequiredService<AlbumService>();
            await albums.RenameAlbumAsync(SelectedAlbum.Id, name.Trim());
            SelectedAlbum.Name = name.Trim();   // 同步界面
        }
        catch (Exception ex)
        {
            await _dialog.InfoAsync("重命名失败", ex.Message);
        }
    }

    //删除选中的相册（级联）
    [RelayCommand]
    private async Task DeleteAlbumAsync()
    {
        if (SelectedAlbum == null)
            return;

        bool ok = await _dialog.ConfirmAsync("删除相册", $"确定删除相册“{SelectedAlbum.Name}”及其所有子相册吗？");
        if (!ok)
            return;

        long id = SelectedAlbum.Id;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var albums = scope.ServiceProvider.GetRequiredService<AlbumService>();
            await albums.DeleteAlbumAsync(id);
        }
        catch (Exception ex)
        {
            await _dialog.InfoAsync("删除失败", ex.Message);
            return;
        }

        SelectedAlbum = null;             // 回到全部视图
        await LoadAlbumTreeAsync();
    }

    //导入照片到当前选中相册（未选中/未分类则落未分类）
    [RelayCommand]
    private async Task ImportPhotosAsync()
    {
        IReadOnlyList<string> files = await _filePicker.PickPhotoFilesAsync();
        if (files.Count == 0)
            return;

        long? albumId = SelectedAlbum?.Id;

        int success = 0;
        using (var scope = _scopeFactory.CreateScope())
        {
            var import = scope.ServiceProvider.GetRequiredService<PhotoImportService>();
            foreach (string file in files)
            {
                try
                {
                    //先查重，重复时问用户是否仍要导入
                    Photo? duplicate = await import.CheckDuplicateAsync(file);
                    bool allow = false;
                    if (duplicate != null)
                    {
                        allow = await _dialog.ConfirmAsync("重复照片", $"“{file}”与已有照片重复，是否仍然导入？");
                        if (!allow)
                            continue;
                    }

                    await import.ImportAsync(file, albumId, null, allow);
                    success++;
                }
                catch (Exception ex)
                {
                    await _dialog.InfoAsync("导入失败", $"{file}\n{ex.Message}");
                }
            }
        }

        await _dialog.InfoAsync("导入完成", $"成功导入 {success}/{files.Count} 张。");
        await LoadPhotosAsync();
    }

    //删除选中的照片
    [RelayCommand]
    private async Task DeletePhotoAsync()
    {
        if (SelectedPhoto == null)
            return;

        bool ok = await _dialog.ConfirmAsync("删除照片", $"确定删除照片“{SelectedPhoto.Name}”吗？");
        if (!ok)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var photos = scope.ServiceProvider.GetRequiredService<PhotoService>();
            await photos.DeletePhotoAsync(SelectedPhoto.Id);
            Photos.Remove(SelectedPhoto);
            SelectedPhoto = null;
        }
        catch (Exception ex)
        {
            await _dialog.InfoAsync("删除失败", ex.Message);
        }
    }

    //加载相册树：取全部相册，在内存里按父子关系组装成树
    private async Task LoadAlbumTreeAsync()
    {
        IsBusy = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var albums = scope.ServiceProvider.GetRequiredService<IAlbumRepository>();

            IReadOnlyList<Album> all = await albums.GetAllAsync();
            Album? unclassified = await albums.GetUnclassifiedAsync();

            //先给每个相册建节点
            var nodes = new Dictionary<long, AlbumNodeViewModel>();
            foreach (Album a in all)
                nodes[a.Id] = new AlbumNodeViewModel(a) { IsUnclassified = a.Id == unclassified?.Id };

            //再按ParentId挂父子关系，找不到父的作为根
            AlbumTree.Clear();
            foreach (AlbumNodeViewModel node in nodes.Values.OrderBy(n => n.Name))
            {
                if (node.ParentId is long pid && nodes.TryGetValue(pid, out AlbumNodeViewModel? parent))
                    parent.Children.Add(node);
                else
                    AlbumTree.Add(node);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    //加载当前视图的照片（全部/未分类/某相册）
    private async Task LoadPhotosAsync()
    {
        IsBusy = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var photoService = scope.ServiceProvider.GetRequiredService<PhotoService>();

            //决定视图类型
            PhotoView view;
            long? albumId = null;
            if (SelectedAlbum == null)
            {
                view = PhotoView.All;
            }
            else if (SelectedAlbum.IsUnclassified)
            {
                view = PhotoView.Unclassified;
            }
            else
            {
                view = PhotoView.Album;
                albumId = SelectedAlbum.Id;
            }

            IReadOnlyList<Photo> list = await photoService.GetPhotosAsync(view, albumId, 0, PageSize);

            //把实体投影成VM对象（实体不保留）
            Photos.Clear();
            foreach (Photo p in list)
            {
                var vm = new PhotoItemViewModel(p.Id, p.Name) { Remark = p.Remark };
                try
                {
                    vm.Thumbnail = await photoService.GetThumbnailAsync(p);
                }
                catch
                {
                    vm.Thumbnail = null;   // 缩略图失败不影响列表显示
                }
                Photos.Add(vm);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
