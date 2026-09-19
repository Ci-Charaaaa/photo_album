namespace PhotoAlbum.ViewModels.Services;

//文件/文件夹选择服务（由表现层实现，VM只面向接口）
public interface IFilePickerService
{
    //选择要导入的照片文件，返回文件绝对路径；取消返回空列表
    Task<IReadOnlyList<string>> PickPhotoFilesAsync();

    //选择文件夹（用于选库根），返回绝对路径；取消返回null
    Task<string?> PickFolderAsync(string title);
}
