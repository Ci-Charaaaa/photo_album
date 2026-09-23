using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace PhotoAlbum.Presentation;

//App 是 Avalonia 的应用对象，负责整体生命周期。
//它有两个关键回调：Initialize（加载 XAML 资源）和 OnFrameworkInitializationCompleted（框架就绪后做事）。
public partial class App : Avalonia.Application
{
    //初始化：加载 App.axaml（里面是全局样式，如 FluentTheme）
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    //框架初始化完成：在这里创建主窗口。
    //这里只负责"造窗口"，真正的依赖注入/数据库迁移/数据加载放在 MainWindow 打开后（见 MainWindow.axaml.cs），
    //因为选库根需要窗口（要弹系统文件夹选择框）。
    public override void OnFrameworkInitializationCompleted()
    {
        //经典桌面生命周期（普通窗口程序）
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
