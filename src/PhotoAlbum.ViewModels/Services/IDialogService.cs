namespace PhotoAlbum.ViewModels.Services;

//弹窗交互服务（由表现层实现，VM只面向接口）
public interface IDialogService
{
    //提示框
    Task InfoAsync(string title, string message);

    //确认框，用户点"确定"返回true
    Task<bool> ConfirmAsync(string title, string message);

    //输入框，返回用户输入；取消返回null
    Task<string?> PromptAsync(string title, string message, string? initialValue = null);
}
