using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using PhotoAlbum.Application;
using PhotoAlbum.Infrastructure;
using PhotoAlbum.Presentation.Services;
using PhotoAlbum.ViewModels;
using PhotoAlbum.ViewModels.Services;

namespace PhotoAlbum.Presentation;

//主窗口。除了"显示界面"，它还承担"组合根(Composition Root)"的职责：
//把 Application / Infrastructure / UI 能力 / ViewModel 全部组装到 DI 容器里，并启动整个流程。
//（组合根只有表现层能当，因为只有它能同时看到所有层、并知道"用户选的库根"。）
public partial class MainWindow : Window
{
    //DI 容器要长期存活（VM 里还持有它的 scopeFactory），用字段保住引用，避免被回收。
    private ServiceProvider? _provider;

    public MainWindow()
    {
        InitializeComponent();      // 加载 MainWindow.axaml（界面）
        Opened += OnOpened;         // 窗口"已打开"后再启动流程（此时才能弹选择框）
    }

    //窗口打开后触发一次启动流程。用 async void 是因为它是事件处理器；
    //第一件事就把自己从事件里摘掉，保证只跑一次。
    private async void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        await InitializeAsync();
    }

    //启动流程（组合根）：
    //  1) 选定"库根"（丙方案：没有就弹框让用户选，并记住）
    //  2) 组装 DI 容器（注册各层 + UI 能力 + VM）
    //  3) 应用数据库迁移（首次运行建库建表+种数据）
    //  4) 绑定 ViewModel 并加载数据
    private async Task InitializeAsync()
    {
        //1. 读设置里的库根；没有或目录已失效，则弹系统文件夹选择框
        var settings = new SettingsStore();
        string? root = settings.LoadLibraryRoot();
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            var picker = new AvaloniaFilePickerService(this);
            root = await picker.PickFolderAsync("请选择照片库文件夹");
            if (root == null)
            {
                Close();   // 用户取消 -> 关闭程序（没有库根无法工作）
                return;
            }
            settings.SaveLibraryRoot(root);   // 记住，下次不再问
        }

        //2. 组装 DI 容器
        var services = new ServiceCollection();
        services.AddApplication();                       // 应用层：用例服务 + StorageLayout
        services.AddInfrastructure(root);                // 基础设施层：DbContext/仓储/文件后端/外部服务
        // UI 能力实现：它们需要窗口(this)，容器无法自动构造，所以直接传入实例
        services.AddSingleton<IDialogService>(new AvaloniaDialogService(this));
        services.AddSingleton<IFilePickerService>(new AvaloniaFilePickerService(this));
        services.AddSingleton<IImageViewerService>(new AvaloniaImageViewerService(this));
        services.AddSingleton<MainViewModel>();          // 主界面 VM
        _provider = services.BuildServiceProvider();     // 把"配方表"编译成容器

        //3. 应用迁移：首次运行会建库、建表、写入"未分类"种子
        await _provider.MigrateDatabaseAsync();

        //4. 从容器取出 VM，设为窗口的 DataContext（界面据此绑定），并触发初始加载
        var vm = _provider.GetRequiredService<MainViewModel>();
        DataContext = vm;
        await vm.LoadCommand.ExecuteAsync(null);
    }

    //双击照片 -> 执行"查看原图"命令。
    //XAML 里没法把事件直接绑到命令，所以在这里手动转一手（事件 -> VM 命令）。
    private void OnPhotoDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.ViewOriginalCommand.Execute(null);
    }
}
