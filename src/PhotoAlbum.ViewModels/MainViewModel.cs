namespace PhotoAlbum.ViewModels;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Application.Services;
using PhotoAlbum.Domain.Entities;
using PhotoAlbum.ViewModels.Services;

//主界面视图模型：持有相册树、当前选中相册、子相册卡片、照片列表，以及各操作命令
//原则：VM不认识数据库/文件，只通过"开scope -> 取应用服务 -> 调用 -> 释放"来干活
public partial class MainViewModel : ObservableObject
{
    //依赖：作用域工厂 + 三个UI能力接口
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialog;
    private readonly IFilePickerService _filePicker;
    private readonly IImageViewerService _viewer;

    //每屏加载的照片数
    private const int PageSize = 40;

    //是否请求"全部相册"画廊（仅在SelectedAlbum为null时用来区分"全部照片"/"全部相册"）
    private bool _galleryRequested;

    //构造函数，传入依赖
    public MainViewModel(IServiceScopeFactory scopeFactory, IDialogService dialog, IFilePickerService filePicker, IImageViewerService viewer)
    {
        _scopeFactory = scopeFactory;
        _dialog = dialog;
        _filePicker = filePicker;
        _viewer = viewer;
    }

    //相册树（根节点集合）
    public ObservableCollection<AlbumNodeViewModel> AlbumTree { get; } = new();

    //当前选中的相册（null表示"全部照片"或"全部相册"视图）
    [ObservableProperty]
    private AlbumNodeViewModel? _selectedAlbum;

    //相册卡片集合（用于"全部相册"画廊，或当前相册的子相册条）
    public ObservableCollection<AlbumCardViewModel> AlbumCards { get; } = new();

    //照片列表
    public ObservableCollection<PhotoItemViewModel> Photos { get; } = new();

    //当前选中的照片
    [ObservableProperty]
    private PhotoItemViewModel? _selectedPhoto;

    //三个区域可见性：子相册条 / 照片区 / 全部相册画廊
    [ObservableProperty]
    private bool _showChildAlbums;

    [ObservableProperty]
    private bool _showPhotos = true;

    [ObservableProperty]
    private bool _showGallery;

    //是否忙碌（供界面显示加载状态）
    [ObservableProperty]
    private bool _isBusy;

    //是否可以返回上级（在相册内时为true）
    [ObservableProperty]
    private bool _canGoBack;

    //选中相册变化时，更新返回键状态并重新加载当前视图
    partial void OnSelectedAlbumChanged(AlbumNodeViewModel? value)
    {
        CanGoBack = value != null;
        //属性变化回调里不能await，用丢弃方式触发
        _ = ReloadCurrentViewAsync();
    }

    //打开某个相册（由相册卡片的命令调用）
    private Task OpenAlbumAsync(AlbumNodeViewModel node)
    {
        SelectedAlbum = node;   // 触发 OnSelectedAlbumChanged 重新加载
        return Task.CompletedTask;
    }

    //初始加载：相册树 + 默认进入"全部相册"画廊
    [RelayCommand]
    private async Task LoadAsync()
    {
        await LoadAlbumTreeAsync();
        _galleryRequested = true;   // 默认进入"全部相册"
        await ReloadCurrentViewAsync();
    }

    //返回上级：有父相册则进父相册；顶层相册则回到"全部相册"画廊
    [RelayCommand]
    private void GoBack()
    {
        if (SelectedAlbum == null)
            return;

        AlbumNodeViewModel? parent = SelectedAlbum.ParentId is long pid
            ? AlbumTree.FirstOrDefault(n => n.Id == pid)
            : null;

        if (parent != null)
        {
            SelectedAlbum = parent;   // 触发重新加载
        }
        else
        {
            _galleryRequested = true;
            SelectedAlbum = null;     // 触发重新加载（回到画廊）
        }
    }

    //显示全部照片
    [RelayCommand]
    private async Task ShowAllAsync()
    {
        _galleryRequested = false;
        if (SelectedAlbum != null)
            SelectedAlbum = null;   // 触发重新加载
        else
            await ReloadCurrentViewAsync();
    }

    //显示"全部相册"画廊（第一级相册，含未分类）
    [RelayCommand]
    private async Task ShowAlbumsAsync()
    {
        _galleryRequested = true;
        if (SelectedAlbum != null)
            SelectedAlbum = null;   // 触发重新加载
        else
            await ReloadCurrentViewAsync();
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
            await ReloadCurrentViewAsync();
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
            await LoadAlbumTreeAsync();
            await ReloadCurrentViewAsync();
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

        SelectedAlbum = null;
        _galleryRequested = false;
        await LoadAlbumTreeAsync();
        await ReloadCurrentViewAsync();
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
        await ReloadCurrentViewAsync();
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

    //查看选中照片的原图
    [RelayCommand]
    private async Task ViewOriginalAsync()
    {
        if (SelectedPhoto == null)
            return;

        try
        {
            byte[] data;
            using (var scope = _scopeFactory.CreateScope())
            {
                var photos = scope.ServiceProvider.GetRequiredService<PhotoService>();
                data = await photos.GetOriginalBytesAsync(SelectedPhoto.Id);
            }
            await _viewer.ViewImageAsync(SelectedPhoto.Name, data);
        }
        catch (Exception ex)
        {
            await _dialog.InfoAsync("查看原图失败", ex.Message);
        }
    }

    //移动选中的照片到指定相册
    [RelayCommand]
    private async Task MovePhotoAsync()
    {
        if (SelectedPhoto == null)
            return;

        List<(long Id, string DisplayName)> albums = BuildAlbumChoices();
        long? target = await _dialog.PickAlbumAsync("移动照片", albums);
        if (target == null)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var photos = scope.ServiceProvider.GetRequiredService<PhotoService>();
            await photos.MovePhotoAsync(SelectedPhoto.Id, target.Value);
            await ReloadCurrentViewAsync();
        }
        catch (Exception ex)
        {
            await _dialog.InfoAsync("移动失败", ex.Message);
        }
    }

    //重命名选中的照片（只改显示名，磁盘文件名不动）
    [RelayCommand]
    private async Task RenamePhotoAsync()
    {
        if (SelectedPhoto == null)
            return;

        string? name = await _dialog.PromptAsync("重命名照片", "请输入新的名称", SelectedPhoto.Name);
        if (string.IsNullOrWhiteSpace(name))
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var photos = scope.ServiceProvider.GetRequiredService<PhotoService>();
            await photos.RenamePhotoAsync(SelectedPhoto.Id, name.Trim());
            SelectedPhoto.Name = name.Trim();
        }
        catch (Exception ex)
        {
            await _dialog.InfoAsync("重命名失败", ex.Message);
        }
    }

    //修改选中照片的备注
    [RelayCommand]
    private async Task UpdatePhotoRemarkAsync()
    {
        if (SelectedPhoto == null)
            return;

        string? remark = await _dialog.PromptAsync("照片备注", "请输入备注", SelectedPhoto.Remark ?? string.Empty, multiline: true);
        if (remark == null)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var photos = scope.ServiceProvider.GetRequiredService<PhotoService>();
            await photos.UpdatePhotoRemarkAsync(SelectedPhoto.Id, remark);
            SelectedPhoto.Remark = remark;
        }
        catch (Exception ex)
        {
            await _dialog.InfoAsync("保存备注失败", ex.Message);
        }
    }

    //修改选中相册的备注（富文本，暂以纯文本编辑）
    [RelayCommand]
    private async Task UpdateAlbumRemarkAsync()
    {
        if (SelectedAlbum == null)
            return;

        string? remark = await _dialog.PromptAsync("相册备注", "请输入备注", SelectedAlbum.Remark ?? string.Empty, multiline: true);
        if (remark == null)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var albums = scope.ServiceProvider.GetRequiredService<AlbumService>();
            await albums.UpdateAlbumRemarkAsync(SelectedAlbum.Id, remark);
            SelectedAlbum.Remark = remark;
        }
        catch (Exception ex)
        {
            await _dialog.InfoAsync("保存备注失败", ex.Message);
        }
    }

    //把相册树拍平成"（id, 缩进名）"列表，供选相册对话框使用
    private List<(long Id, string DisplayName)> BuildAlbumChoices()
    {
        var choices = new List<(long, string)>();

        void Walk(AlbumNodeViewModel node, int depth)
        {
            choices.Add((node.Id, new string(' ', depth * 2) + node.Name));
            foreach (AlbumNodeViewModel child in node.Children)
                Walk(child, depth + 1);
        }

        foreach (AlbumNodeViewModel root in AlbumTree)
            Walk(root, 0);

        return choices;
    }

    //重新加载当前视图：全部照片 / 全部相册画廊 / 某相册（子相册条 + 照片）
    private async Task ReloadCurrentViewAsync()
    {
        IsBusy = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var photoService = scope.ServiceProvider.GetRequiredService<PhotoService>();

            if (SelectedAlbum == null)
            {
                if (_galleryRequested)
                    await ShowTopLevelAlbumsAsync(photoService);
                else
                    await ShowAllPhotosAsync(photoService);
            }
            else
            {
                await ShowAlbumContentAsync(SelectedAlbum, photoService);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    //全部照片：只显示照片
    private async Task ShowAllPhotosAsync(PhotoService photoService)
    {
        AlbumCards.Clear();
        ShowChildAlbums = false;
        ShowGallery = false;
        ShowPhotos = true;
        await FillPhotosAsync(photoService, PhotoView.All, null);
    }

    //全部相册：第一级相册（含未分类）的卡片墙
    private async Task ShowTopLevelAlbumsAsync(PhotoService photoService)
    {
        ShowChildAlbums = false;
        ShowPhotos = false;
        ShowGallery = true;

        AlbumCards.Clear();
        foreach (AlbumNodeViewModel node in AlbumTree)   // AlbumTree顶层就是第一级
            AlbumCards.Add(await BuildCardAsync(node, photoService));
    }

    //某相册内容：先子相册卡片，再照片
    private async Task ShowAlbumContentAsync(AlbumNodeViewModel album, PhotoService photoService)
    {
        AlbumCards.Clear();
        foreach (AlbumNodeViewModel child in album.Children)
            AlbumCards.Add(await BuildCardAsync(child, photoService));

        ShowChildAlbums = AlbumCards.Count > 0;
        ShowGallery = false;
        ShowPhotos = true;

        if (album.IsUnclassified)
            await FillPhotosAsync(photoService, PhotoView.Unclassified, null);
        else
            await FillPhotosAsync(photoService, PhotoView.Album, album.Id);
    }

    //构建一张相册卡片（含封面与打开命令）
    private async Task<AlbumCardViewModel> BuildCardAsync(AlbumNodeViewModel node, PhotoService photoService)
    {
        var card = new AlbumCardViewModel(node.Id, node.Name)
        {
            OpenCommand = new AsyncRelayCommand(() => OpenAlbumAsync(node))
        };
        card.Thumbnail = await ResolveCoverAsync(node, photoService);
        return card;
    }

    //解析相册封面：本相册有照片用第一张；否则递归找子相册；全树无照片返回null
    private async Task<byte[]?> ResolveCoverAsync(AlbumNodeViewModel node, PhotoService photoService)
    {
        IReadOnlyList<Photo> own = node.IsUnclassified
            ? await photoService.GetPhotosAsync(PhotoView.Unclassified, null, 0, 1)
            : await photoService.GetPhotosAsync(PhotoView.Album, node.Id, 0, 1);

        if (own.Count > 0)
        {
            try { return await photoService.GetThumbnailAsync(own[0]); }
            catch { return null; }
        }

        //没有照片就找子相册
        foreach (AlbumNodeViewModel child in node.Children)
        {
            byte[]? cover = await ResolveCoverAsync(child, photoService);
            if (cover != null)
                return cover;
        }

        return null;
    }

    //把照片实体投影成VM对象填进列表
    private async Task FillPhotosAsync(PhotoService photoService, PhotoView view, long? albumId)
    {
        IReadOnlyList<Photo> list = await photoService.GetPhotosAsync(view, albumId, 0, PageSize);

        SelectedPhoto = null;   // 先清选择，避免集合清空时选中索引越界
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
}
