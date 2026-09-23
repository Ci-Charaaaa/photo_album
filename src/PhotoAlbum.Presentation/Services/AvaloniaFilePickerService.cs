namespace PhotoAlbum.Presentation.Services;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using PhotoAlbum.ViewModels.Services;

//IFilePickerService 的 Avalonia 实现：调用操作系统的文件/文件夹选择框。
//通过 Window.StorageProvider 访问平台选择器（Avalonia 的跨平台抽象）。
public class AvaloniaFilePickerService : IFilePickerService
{
    private readonly Window _owner;   // 选择框需要父窗口

    public AvaloniaFilePickerService(Window owner)
    {
        _owner = owner;
    }

    //选择要导入的照片文件（可多选），返回绝对路径列表
    public async Task<IReadOnlyList<string>> PickPhotoFilesAsync()
    {
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择要导入的照片",
            AllowMultiple = true,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }   // 只筛图片类型
        });

        //IStorageFile -> 本地路径字符串
        return files.Select(f => f.Path.LocalPath).ToList();
    }

    //选择文件夹（用于选"库根"），取消返回 null
    public async Task<string?> PickFolderAsync(string title)
    {
        var folders = await _owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }
}
