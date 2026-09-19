namespace PhotoAlbum.Presentation.Services;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using PhotoAlbum.ViewModels.Services;

//用Avalonia的系统选择器实现的文件/文件夹选择服务
public class AvaloniaFilePickerService : IFilePickerService
{
    private readonly Window _owner;

    public AvaloniaFilePickerService(Window owner)
    {
        _owner = owner;
    }

    //选择要导入的照片文件，返回绝对路径列表
    public async Task<IReadOnlyList<string>> PickPhotoFilesAsync()
    {
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择要导入的照片",
            AllowMultiple = true,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        return files.Select(f => f.Path.LocalPath).ToList();
    }

    //选择文件夹（用于选库根），取消返回null
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
