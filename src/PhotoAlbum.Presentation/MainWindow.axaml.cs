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

public partial class MainWindow : Window
{
    //容器要长期存活，用字段保住引用
    private ServiceProvider? _provider;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    //窗口打开后启动：选定库根 -> 建容器 -> 迁移 -> 绑定VM -> 加载
    private async void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        //1. 读取设置里的库根；没有或失效则让用户选（丙方案），存回设置
        var settings = new SettingsStore();
        string? root = settings.LoadLibraryRoot();
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            var picker = new AvaloniaFilePickerService(this);
            root = await picker.PickFolderAsync("请选择照片库文件夹");
            if (root == null)
            {
                Close();
                return;
            }
            settings.SaveLibraryRoot(root);
        }

        //2. 组装DI容器
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddInfrastructure(root);
        services.AddSingleton<IDialogService>(new AvaloniaDialogService(this));
        services.AddSingleton<IFilePickerService>(new AvaloniaFilePickerService(this));
        services.AddSingleton<MainViewModel>();
        _provider = services.BuildServiceProvider();

        //3. 应用数据库迁移（首次运行会建库建表）
        await _provider.MigrateDatabaseAsync();

        //4. 绑定VM并加载数据
        var vm = _provider.GetRequiredService<MainViewModel>();
        DataContext = vm;
        await vm.LoadCommand.ExecuteAsync(null);
    }
}
