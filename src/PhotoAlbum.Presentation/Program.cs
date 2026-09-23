using Avalonia;
using System;

namespace PhotoAlbum.Presentation;

//程序入口。Avalonia 应用从这里启动。
class Program
{
    //[STAThread]：桌面 UI 需要单线程单元(STA)，这是 Windows 桌面程序的惯例。
    //流程：BuildAvaloniaApp() 配置好应用 -> StartWithClassicDesktopLifetime 启动"经典桌面"生命周期
    //（即普通窗口程序，有主窗口、消息循环），并把命令行参数传进去。
    //注意：在 AppMain 被调用前，别使用任何 Avalonia / 第三方 API，否则可能因未初始化而崩。
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    //构建 Avalonia 应用（不要删；可视化设计器也用它）
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()          // 用我们的 App 类（见 App.axaml.cs）
            .UsePlatformDetect()                // 自动检测并加载对应平台后端（Windows/…）
#if DEBUG
            .WithDeveloperTools()               // 仅 Debug：开启开发者工具
#endif
            .WithInterFont()                    // 使用 Inter 字体
            .LogToTrace();                      // 日志输出到 Trace
}
