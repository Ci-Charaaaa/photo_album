namespace PhotoAlbum.Presentation.Services;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PhotoAlbum.ViewModels.Services;

//用Avalonia窗口实现的弹窗服务（提示/确认/输入/选相册）
public class AvaloniaDialogService : IDialogService
{
    private readonly Window _owner;

    public AvaloniaDialogService(Window owner)
    {
        _owner = owner;
    }

    //提示框
    public async Task InfoAsync(string title, string message)
    {
        var (window, _, buttons) = CreateDialog(title, message);
        var ok = new Button { Content = "确定", MinWidth = 80 };
        ok.Click += (_, _) => window.Close();
        buttons.Children.Add(ok);
        await window.ShowDialog(_owner);
    }

    //确认框
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

    //输入框（multiline=true用多行文本框）
    public async Task<string?> PromptAsync(string title, string message, string? initialValue = null, bool multiline = false)
    {
        var (window, panel, buttons) = CreateDialog(title, message);

        var input = new TextBox
        {
            Text = initialValue ?? string.Empty,
            MinWidth = 300,
            AcceptsReturn = multiline,
            TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            Height = multiline ? 120 : double.NaN
        };
        panel.Children.Insert(1, input);   // 插到"消息文本"和"按钮行"之间

        string? result = null;
        var ok = new Button { Content = "确定", MinWidth = 80 };
        ok.Click += (_, _) => { result = input.Text; window.Close(); };
        var cancel = new Button { Content = "取消", MinWidth = 80 };
        cancel.Click += (_, _) => window.Close();

        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        await window.ShowDialog(_owner);
        return result;
    }

    //选相册：下拉框选择，返回相册id
    public async Task<long?> PickAlbumAsync(string title, IReadOnlyList<(long Id, string DisplayName)> albums)
    {
        var (window, panel, buttons) = CreateDialog(title, "请选择目标相册");

        // 只放显示名；选中后用索引回查id（避免绑定成员名的麻烦）
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

    //构建弹窗：内容 = [消息文本, (可选内容), 按钮行]
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
            Children = { text, buttons }
        };

        var window = new Window
        {
            Title = title,
            Content = panel,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        return (window, panel, buttons);
    }
}
