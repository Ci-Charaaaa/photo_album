using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using PhotoAlbum.Application;
using PhotoAlbum.Infrastructure;
using PhotoAlbum.Presentation.Services;
using PhotoAlbum.ViewModels;
using PhotoAlbum.ViewModels.Services;

namespace PhotoAlbum.Presentation;

//主窗口。除了"显示界面"，它还承担"组合根(Composition Root)"的职责：
//把 Application / Infrastructure / UI 能力 / ViewModel 全部组装到 DI 容器里，并启动整个流程。
public partial class MainWindow : Window
{
    //DI 容器要长期存活（VM 里还持有它的 scopeFactory），用字段保住引用。
    private ServiceProvider? _provider;

    //框选状态：起点（相对于 SelectionCanvas）与是否正在框选
    private Point _marqueeStart;
    private bool _marqueeActive;

    public MainWindow()
    {
        InitializeComponent();      // 加载 MainWindow.axaml（界面）
        Opened += OnOpened;         // 窗口"已打开"后再启动流程（此时才能弹选择框）

        //在照片列表上挂指针事件，实现"右键拖拽框选"。
        //用 Tunnel(隧道)阶段：从外向内传递，能在 ListBox 自己处理之前先拿到事件。
        PhotoList.AddHandler(PointerPressedEvent, OnPhotoPointerPressed, RoutingStrategies.Tunnel);
        PhotoList.AddHandler(PointerMovedEvent, OnPhotoPointerMoved, RoutingStrategies.Tunnel);
        PhotoList.AddHandler(PointerReleasedEvent, OnPhotoPointerReleased, RoutingStrategies.Tunnel);
    }

    //窗口打开后触发一次启动流程（只跑一次）
    private async void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        await InitializeAsync();
    }

    //启动流程（组合根）：选库根 -> 建容器 -> 迁移 -> 绑定VM -> 加载
    private async Task InitializeAsync()
    {
        //1. 读设置里的库根；没有或目录已失效，则弹系统文件夹选择框（丙方案）
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

        //2. 组装 DI 容器
        var services = new ServiceCollection();
        services.AddApplication();                       // 应用层
        services.AddInfrastructure(root);                // 基础设施层
        // UI 能力实现：需要窗口(this)，容器无法自动构造，所以直接传实例
        services.AddSingleton<IDialogService>(new AvaloniaDialogService(this));
        services.AddSingleton<IFilePickerService>(new AvaloniaFilePickerService(this));
        services.AddSingleton<IImageViewerService>(new AvaloniaImageViewerService(this));
        services.AddSingleton<MainViewModel>();
        _provider = services.BuildServiceProvider();

        //3. 应用数据库迁移
        await _provider.MigrateDatabaseAsync();

        //4. 绑定 VM 并加载
        var vm = _provider.GetRequiredService<MainViewModel>();
        DataContext = vm;
        await vm.LoadCommand.ExecuteAsync(null);
    }

    //双击照片 -> 查看原图
    private void OnPhotoDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.ViewOriginalCommand.Execute(null);
    }

    //照片列表的选中变化 -> 同步进 VM.SelectedPhotos（供批量删除/移动）。
    //无论是用户 Ctrl/Shift 多选，还是代码框选后改 SelectedItems，都会走到这里。
    private void OnPhotoSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        vm.SelectedPhotos.Clear();
        if (PhotoList.SelectedItems is { } items)
        {
            foreach (object? item in items)
                if (item is PhotoItemViewModel p)
                    vm.SelectedPhotos.Add(p);
        }
    }

    //按下：只有"右键按下"才开始框选
    private void OnPhotoPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(PhotoList).Properties.IsRightButtonPressed)
            return;

        _marqueeActive = true;
        _marqueeStart = e.GetPosition(SelectionCanvas);
        SelectionRect.IsVisible = true;
        UpdateMarquee(_marqueeStart);
        e.Handled = true;   // 拦下，别让 ListBox 再处理
    }

    //移动：更新矩形大小
    private void OnPhotoPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_marqueeActive)
            return;

        UpdateMarquee(e.GetPosition(SelectionCanvas));
        e.Handled = true;
    }

    //松开：结束框选，算出矩形内的照片并选中
    private void OnPhotoPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_marqueeActive)
            return;

        _marqueeActive = false;
        SelectionRect.IsVisible = false;

        double left = Canvas.GetLeft(SelectionRect);
        double top = Canvas.GetTop(SelectionRect);
        double width = SelectionRect.Width;
        double height = SelectionRect.Height;

        //太小的当误触，忽略
        if (width < 4 || height < 4)
        {
            e.Handled = true;
            return;
        }

        var marquee = new Rect(left, top, width, height);

        //逐个照片控件判断：它的边界框和框选矩形是否相交
        var hit = new List<object>();
        for (int i = 0; i < PhotoList.ItemCount; i++)
        {
            if (PhotoList.ContainerFromIndex(i) is not Control container)
                continue;

            //把控件左上角换算到 SelectionCanvas 坐标系（自动考虑滚动偏移）
            Point? origin = container.TranslatePoint(new Point(0, 0), SelectionCanvas);
            if (origin is null)
                continue;

            var bounds = new Rect(origin.Value, container.Bounds.Size);
            if (marquee.Intersects(bounds))
                hit.Add(PhotoList.Items[i]!);
        }

        //用控制器的 SelectedItems 执行选中（会触发 SelectionChanged -> 同步进 VM）
        PhotoList.SelectedItems ??= new List<object>();
        PhotoList.SelectedItems.Clear();
        foreach (object item in hit)
            PhotoList.SelectedItems.Add(item);

        e.Handled = true;
    }

    //根据起点与当前点，更新矩形的位置和大小（支持任意方向拖拽）
    private void UpdateMarquee(Point current)
    {
        double x = Math.Min(_marqueeStart.X, current.X);
        double y = Math.Min(_marqueeStart.Y, current.Y);
        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = Math.Abs(current.X - _marqueeStart.X);
        SelectionRect.Height = Math.Abs(current.Y - _marqueeStart.Y);
    }
}
