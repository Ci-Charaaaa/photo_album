// ============================================================================
// MainViewModel —— 主界面视图模型
// ----------------------------------------------------------------------------
// 职责：把"后端数据"变成界面能绑定的形状，把"界面操作"变成对后端的调用
// 两条铁律：
//   1. VM不认识数据库/文件，所有后端操作都通过"应用服务"完成；
//   2. VM是可测试的纯逻辑，它不引用 Avalonia，窗口/弹窗等UI能力都走接口(见Services)
//
// 关于"每操作一个 scope"：
//   - VM活得久（整个窗口生命周期），而DbContext/用例服务活得短（一次操作一个）；
//   - 所以VM不长期持有它们，而是注入IServiceScopeFactory，
//     每次干活时：CreateScope() -> 取服务 -> 调用 -> using自动释放。
//
// 关于 [ObservableProperty] / [RelayCommand]：
//   - 它们是 CommunityToolkit.Mvvm 的"源生成器"，编译时自动生成属性/命令，
//     并在赋值时发通知（PropertyChanged），界面据此自动刷新
//   - 所以下面的_selectedAlbum字段，实际会生成一个SelectedAlbum属性。
// ============================================================================
namespace PhotoAlbum.ViewModels;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Application.Services;
using PhotoAlbum.Domain.Entities;
using PhotoAlbum.ViewModels.Services;

//主界面视图模型
public partial class MainViewModel : ObservableObject   // 继承通知基类，属性变化能通知界面
{
    // ── 依赖（全部是接口，由DI注入，VM不认识具体实现）──────────────
    private readonly IServiceScopeFactory _scopeFactory;  //用来手动开作用域，临时取应用服务
    private readonly IDialogService _dialog;              //弹窗（提示/确认/输入/选相册）
    private readonly IFilePickerService _filePicker;      //选文件/文件夹
    private readonly IImageViewerService _viewer;         //弹窗看原图

    //每屏加载的照片数（首屏约40张）
    private const int PageSize = 40;

    //私有状态：是否请求"全部相册"画廊。
    //因为"全部照片"和"全部相册"都用 SelectedAlbum==null 表示，需要这个开关区分。
    private bool _galleryRequested;

    //构造函数：只接收依赖并存起来，不写任何业务逻辑
    public MainViewModel(IServiceScopeFactory scopeFactory, IDialogService dialog, IFilePickerService filePicker, IImageViewerService viewer)
    {
        _scopeFactory = scopeFactory;
        _dialog = dialog;
        _filePicker = filePicker;
        _viewer = viewer;
    }

    // ── 状态属性（界面绑定它们）────────────────────────────────────

    //相册树（根节点集合），Children里递归挂子相册，构成树
    //ObservableCollection：增删元素时自动通知界面
    public ObservableCollection<AlbumNodeViewModel> AlbumTree { get; } = new();

    //当前选中的相册（null表示"全部照片"或"全部相册"视图）
    //[ObservableProperty] 会给这个字段生成 SelectedAlbum 属性 + 变化通知 + 变化回调。
    [ObservableProperty]
    private AlbumNodeViewModel? _selectedAlbum;

    //相册卡片集合：用于"全部相册"画廊，或"某相册的子相册条"
    public ObservableCollection<AlbumCardViewModel> AlbumCards { get; } = new();

    //照片列表（当前视图下的照片）
    public ObservableCollection<PhotoItemViewModel> Photos { get; } = new();

    //当前选中的照片
    [ObservableProperty]
    private PhotoItemViewModel? _selectedPhoto;

    //三个互斥区域的可见性（界面用 IsVisible 绑定）：
    // 子相册条（相册含子相册时）、照片区、全部相册画廊
    [ObservableProperty]
    private bool _showChildAlbums;

    [ObservableProperty]
    private bool _showPhotos = true;   //默认显示照片区

    [ObservableProperty]
    private bool _showGallery;

    //是否忙碌（可用于界面禁用/转圈）
    [ObservableProperty]
    private bool _isBusy;

    //能否返回上级（在相册内时为 true，界面据此决定"返回"按钮是否显示）
    [ObservableProperty]
    private bool _canGoBack;

    // ── 属性变化回调（由源生成器调用）──────────────────────────────

    //当SelectedAlbum变化时触发：
    //更新返回键状态，并异步重载当前视图（回调里不能await，用 `_ =` 触发）。
    partial void OnSelectedAlbumChanged(AlbumNodeViewModel? value)
    {
        CanGoBack = value != null;
        _ = ReloadCurrentViewAsync();
    }

    //打开某个相册（由相册卡片的命令调用）：选中它即可，剩下的交给上面的回调
    private Task OpenAlbumAsync(AlbumNodeViewModel node)
    {
        SelectedAlbum = node;   // 赋值 -> 触发 OnSelectedAlbumChanged -> 重新加载
        return Task.CompletedTask;
    }

    // ── 命令（界面按钮/交互绑定它们）────────────────────────────────
    // [RelayCommand] 会把方法生成成XXXCommand属性（async方法生成AsyncRelayCommand）。

    //初始加载：先取相册树，再默认进入"全部相册"画廊
    [RelayCommand]
    private async Task LoadAsync()
    {
        await LoadAlbumTreeAsync();
        _galleryRequested = true;   // 默认进画廊
        await ReloadCurrentViewAsync();
    }

    //返回上级：有父相册则进父相册；顶层相册则回到"全部相册"画廊
    [RelayCommand]
    private void GoBack()
    {
        if (SelectedAlbum == null)
            return;

        //在树里找父节点（通过ParentId）
        AlbumNodeViewModel? parent = SelectedAlbum.ParentId is long pid
            ? AlbumTree.FirstOrDefault(n => n.Id == pid)
            : null;

        if (parent != null)
        {
            SelectedAlbum = parent;   //赋值触发重新加载
        }
        else
        {
            _galleryRequested = true;
            SelectedAlbum = null;     //赋值触发重新加载（回到画廊）
        }
    }

    //显示全部照片
    [RelayCommand]
    private async Task ShowAllAsync()
    {
        _galleryRequested = false;
        if (SelectedAlbum != null)
            SelectedAlbum = null;   //触发重新加载
        else
            await ReloadCurrentViewAsync();   //本来就在null，手动加载
    }

    //显示"全部相册"画廊（第一级相册，含未分类）
    [RelayCommand]
    private async Task ShowAlbumsAsync()
    {
        _galleryRequested = true;
        if (SelectedAlbum != null)
            SelectedAlbum = null;   //触发重新加载
        else
            await ReloadCurrentViewAsync();
    }

    //新建相册：在当前选中相册下建子相册（未选中则建成根相册）
    [RelayCommand]
    private async Task CreateAlbumAsync()
    {
        string? name = await _dialog.PromptAsync("新建相册", "请输入相册名称", string.Empty);
        if (string.IsNullOrWhiteSpace(name))
            return;   //用户取消或没输入

        long? parentId = SelectedAlbum?.Id;
        try
        {
            //开作用域 -> 取用例服务 -> 调用 -> using结束自动释放
            using var scope = _scopeFactory.CreateScope();
            var albums = scope.ServiceProvider.GetRequiredService<AlbumService>();
            await albums.CreateAlbumAsync(name.Trim(), parentId);

            //数据变了，重新加载树和当前视图
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

            await LoadAlbumTreeAsync();       //名字变了，树要重建
            await ReloadCurrentViewAsync();
        }
        catch (Exception ex)
        {
            await _dialog.InfoAsync("重命名失败", ex.Message);
        }
    }

    //删除选中的相册（级联：连同子相册一起删）
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

        //删除成功后回到"全部照片"，重建树和视图
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
            return;   //用户没选文件

        long? albumId = SelectedAlbum?.Id;   //null时由应用服务兜底到"未分类"

        int success = 0;
        //整批导入共用一个 scope（同一批导入共享上下文，更一致）
        using (var scope = _scopeFactory.CreateScope())
        {
            var import = scope.ServiceProvider.GetRequiredService<PhotoImportService>();
            foreach (string file in files)
            {
                try
                {
                    //先查重：内容哈希命中已有照片时，问用户是否仍要导入
                    Photo? duplicate = await import.CheckDuplicateAsync(file);
                    bool allow = false;
                    if (duplicate != null)
                    {
                        allow = await _dialog.ConfirmAsync("重复照片", $"“{file}”与已有照片重复，是否仍然导入？");
                        if (!allow)
                            continue;
                    }

                    //导入（allowDuplicate 决定是否放行重复）
                    await import.ImportAsync(file, albumId, null, allow);
                    success++;
                }
                catch (Exception ex)
                {
                    //单张失败不影响其它，弹提示后继续
                    await _dialog.InfoAsync("导入失败", $"{file}\n{ex.Message}");
                }
            }
        }

        await _dialog.InfoAsync("导入完成", $"成功导入 {success}/{files.Count} 张。");
        await ReloadCurrentViewAsync();   // 有新照片了，刷新
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

            //本地直接从列表移除，省一次全量重载
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
                data = await photos.GetOriginalBytesAsync(SelectedPhoto.Id);   // 读原图字节
            }
            //交给表现层的查看器窗口显示
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

        //把相册树拍平成选项列表，弹框让用户选目标相册
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
            SelectedPhoto.Name = name.Trim();   //同步界面（可观察属性）
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

        //多行输入框；取消返回null（区别于"清空备注"返回空串，所以用 null 判断取消）
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

    //修改选中相册的备注（需求要求富文本，当前用纯文本编辑代替）
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

    // ── 私有辅助方法（加载/投影）────────────────────────────────────

    //把相册树拍平成"（id, 缩进名）"列表，供选相册对话框使用
    //缩进用空格表示层级，纯展示用
    private List<(long Id, string DisplayName)> BuildAlbumChoices()
    {
        var choices = new List<(long, string)>();

        //局部递归函数：先序深度优先，按深度加缩进
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

    //重新加载当前视图。根据状态分派到三种：
    //  1) 全部照片   2) 全部相册画廊   3) 某相册（子相册条 + 照片）
    private async Task ReloadCurrentViewAsync()
    {
        IsBusy = true;
        try
        {
            //一次重载共用一个scope，避免为每个子步骤重复开
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

    //"全部照片"：只显示照片，隐藏卡片区
    private async Task ShowAllPhotosAsync(PhotoService photoService)
    {
        AlbumCards.Clear();
        ShowChildAlbums = false;
        ShowGallery = false;
        ShowPhotos = true;
        await FillPhotosAsync(photoService, PhotoView.All, null);
    }

    //"全部相册"：把 AlbumTree 的第一级节点做成卡片墙（含未分类）
    private async Task ShowTopLevelAlbumsAsync(PhotoService photoService)
    {
        ShowChildAlbums = false;
        ShowPhotos = false;
        ShowGallery = true;

        AlbumCards.Clear();
        foreach (AlbumNodeViewModel node in AlbumTree)   //AlbumTree 顶层就是第一级
            AlbumCards.Add(await BuildCardAsync(node, photoService));
    }

    //"某相册"：先放子相册卡片（有才显示子相册条），再放本相册照片
    private async Task ShowAlbumContentAsync(AlbumNodeViewModel album, PhotoService photoService)
    {
        AlbumCards.Clear();
        foreach (AlbumNodeViewModel child in album.Children)
            AlbumCards.Add(await BuildCardAsync(child, photoService));

        ShowChildAlbums = AlbumCards.Count > 0;   //没子相册就不显示那条
        ShowGallery = false;
        ShowPhotos = true;

        //未分类视图只显示"主相册是未分类"的照片；普通相册显示主+附加
        if (album.IsUnclassified)
            await FillPhotosAsync(photoService, PhotoView.Unclassified, null);
        else
            await FillPhotosAsync(photoService, PhotoView.Album, album.Id);
    }

    //构建一张相册卡片：注入"打开命令" + 解析封面
    private async Task<AlbumCardViewModel> BuildCardAsync(AlbumNodeViewModel node, PhotoService photoService)
    {
        var card = new AlbumCardViewModel(node.Id, node.Name)
        {
            //点击卡片时执行：打开该相册（命令闭包捕获了 node）
            OpenCommand = new AsyncRelayCommand(() => OpenAlbumAsync(node))
        };
        card.Thumbnail = await ResolveCoverAsync(node, photoService);
        return card;
    }

    //解析相册封面（递归）：
    //  本相册有照片 -> 用第一张的缩略图；
    //  否则递归找子相册的封面；
    //  整棵子树都没照片 -> 返回 null（卡片显示深色空底）
    private async Task<byte[]?> ResolveCoverAsync(AlbumNodeViewModel node, PhotoService photoService)
    {
        IReadOnlyList<Photo> own = node.IsUnclassified
            ? await photoService.GetPhotosAsync(PhotoView.Unclassified, null, 0, 1)
            : await photoService.GetPhotosAsync(PhotoView.Album, node.Id, 0, 1);

        if (own.Count > 0)
        {
            try { return await photoService.GetThumbnailAsync(own[0]); }
            catch { return null; }   // 缩略图失败就当没有
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

    //把"照片实体"投影成"照片VM对象"填进列表（实体是过路数据，投影后即弃）
    private async Task FillPhotosAsync(PhotoService photoService, PhotoView view, long? albumId)
    {
        IReadOnlyList<Photo> list = await photoService.GetPhotosAsync(view, albumId, 0, PageSize);

        //先清选中再清集合：避免集合清空时 ListBox 的选中索引越界
        SelectedPhoto = null;
        Photos.Clear();
        foreach (Photo p in list)
        {
            //实体 -> VM：只取界面要用的字段
            var vm = new PhotoItemViewModel(p.Id, p.Name) { Remark = p.Remark };
            try
            {
                vm.Thumbnail = await photoService.GetThumbnailAsync(p);   // 触发缩略图（未命中会生成）
            }
            catch
            {
                vm.Thumbnail = null;   // 缩略图失败不影响列表显示
            }
            Photos.Add(vm);
        }
    }

    //加载相册树：从仓储取全部相册，在内存里按ParentId组装成树
    private async Task LoadAlbumTreeAsync()
    {
        IsBusy = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var albums = scope.ServiceProvider.GetRequiredService<IAlbumRepository>();

            IReadOnlyList<Album> all = await albums.GetAllAsync();
            Album? unclassified = await albums.GetUnclassifiedAsync();   //用来标记哪个是"未分类"

            //第一步：每个相册先建一个节点，放进字典（方便按 id 找）
            var nodes = new Dictionary<long, AlbumNodeViewModel>();
            foreach (Album a in all)
                nodes[a.Id] = new AlbumNodeViewModel(a) { IsUnclassified = a.Id == unclassified?.Id };

            //第二步：按ParentId挂到父节点的Children；找不到父的当作根
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
