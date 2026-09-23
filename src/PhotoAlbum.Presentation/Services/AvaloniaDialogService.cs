namespace PhotoAlbum.Presentation.Services;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PhotoAlbum.ViewModels.Services;

//IDialogService 的 Avalonia 实现：用代码动态构建弹窗窗口。
//为什么用代码建窗而不是 XAML？——弹窗很小、复用同一套布局，代码构建更集中、少几个文件。
//所有弹窗都是"模态"的：用 ShowDialog(owner) 会阻塞（await）直到关闭。
public class AvaloniaDialogService : IDialogService
{
    private readonly Window _owner;   // 弹窗的父窗口（用于居中、模态）

    public AvaloniaDialogService(Window owner)
    {
        _owner = owner;
    }

    //提示框：一个"确定"按钮
    public async Task InfoAsync(string title, string message)
    {
        var (window, _, buttons) = CreateDialog(title, message);
        var ok = new Button { Content = "确定", MinWidth = 80 };
        ok.Click += (_, _) => window.Close();
        buttons.Children.Add(ok);
        await window.ShowDialog(_owner);
    }

    //确认框：确定/取消；点确定把 result 置 true（用闭包把结果带出去）
    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var (window, _, buttons) = CreateDialog(title, message);
        bool result = false;

        var ok = new Button { Content = "确定", MinWidth = 80 };
        ok.Click += (_, _) => { result = true; window.Close(); };
        var cancel = new Button { Content = "取消", MinWidth = 80 };
        cancel.Click += (_, _) => window.Close();

        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        await window.ShowDialog(_owner);
        return result;
    }

    //输入框：multiline=true 用多行文本框（备注用）
    public async Task<string?> PromptAsync(string title, string message, string? initialValue = null, bool multiline = false)
    {
        var (window, panel, buttons) = CreateDialog(title, message);

        var input = new TextBox
        {
            Text = initialValue ?? string.Empty,
            MinWidth = 300,
            AcceptsReturn = multiline,   // 多行时允许回车换行
            TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            Height = multiline ? 120 : double.NaN   // NaN = 由内容决定高度
        };
        panel.Children.Insert(1, input);   // 插到"消息文本"和"按钮行"之间（索引1）

        string? result = null;             // null 表示"取消"
        var ok = new Button { Content = "确定", MinWidth = 80 };
        ok.Click += (_, _) => { result = input.Text; window.Close(); };
        var cancel = new Button { Content = "取消", MinWidth = 80 };
        cancel.Click += (_, _) => window.Close();

        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        await window.ShowDialog(_owner);
        return result;
    }

    //选相册：下拉框选一个，返回其 id
    public async Task<long?> PickAlbumAsync(string title, IReadOnlyList<(long Id, string DisplayName)> albums)
    {
        var (window, panel, buttons) = CreateDialog(title, "请选择目标相册");

        //只把"显示名"放进下拉框；选中后用"索引"回查 id。
        //这样避免处理"绑定显示成员"的麻烦。
        var names = albums.Select(a => a.DisplayName).ToList();
        var combo = new ComboBox
        {
            ItemsSource = names,
            SelectedIndex = names.Count > 0 ? 0 : -1,
            MinWidth = 300,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        panel.Children.Insert(1, combo);

        long? result = null;
        var ok = new Button { Content = "确定", MinWidth = 80 };
        ok.Click += (_, _) =>
        {
            int index = combo.SelectedIndex;
            if (index >= 0 && index < albums.Count)
                result = albums[index].Id;
            window.Close();
        };
        var cancel = new Button { Content = "取消", MinWidth = 80 };
        cancel.Click += (_, _) => window.Close();

        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        await window.ShowDialog(_owner);
        return result;
    }

    //构建弹窗骨架：内容 = [消息文本, (可选内容占位在索引1), 按钮行]
    //返回三个引用，方便调用方插入自己的内容、添加按钮。
    private static (Window Window, StackPanel Panel, StackPanel Buttons) CreateDialog(string title, string message)
    {
        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 420
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 12,
            Children = { text, buttons }   // 此时索引0=文本，索引1=按钮行
        };

        var window = new Window
        {
            Title = title,
            Content = panel,
            SizeToContent = SizeToContent.WidthAndHeight,   // 窗口自适应内容
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        return (window, panel, buttons);
    }
}
