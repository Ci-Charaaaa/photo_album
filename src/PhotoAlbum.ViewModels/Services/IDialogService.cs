namespace PhotoAlbum.ViewModels.Services;

//弹窗交互服务的接口
//为什么定义成接口、而不直接在 VM 里弹窗？
//  1. VM 要保持"与 UI 框架无关"，这样才能单独跑单元测试（测试时注入假的 IDialogService）；
//  2. 具体弹窗要用 Avalonia 的窗口，属于"表现层"的事，所以"接口在 VM、实现在 Presentation"；
//  3. 依赖倒置：上层(VM)面向契约，下层(Presentation)提供实现，由 DI 接上。
public interface IDialogService
{
    //提示框（只有一个"确定"）
    Task InfoAsync(string title, string message);

    //确认框：点"确定"返回true，点"取消"返回false
    Task<bool> ConfirmAsync(string title, string message);

    //输入框：返回用户输入；点"取消"返回null。
    //  initialValue：初始文本；multiline：是否多行（用于编辑备注）
    //  用null表示"取消"，从而与"用户把内容清空后确定（返回空串）"区分开
    Task<string?> PromptAsync(string title, string message, string? initialValue = null, bool multiline = false);

    //从相册列表里选一个，返回相册id；取消返回null。
    //  入参是"（id, 显示名）"列表，显示名可带缩进表示层级
    Task<long?> PickAlbumAsync(string title, IReadOnlyList<(long Id, string DisplayName)> albums);
}
