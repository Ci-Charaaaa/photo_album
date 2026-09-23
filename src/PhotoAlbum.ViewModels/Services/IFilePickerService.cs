namespace PhotoAlbum.ViewModels.Services;

//文件/文件夹选择服务——接口。
//同IDialogService：接口在 VM、实现在Presentation（因为要调用操作系统的选择框）。
public interface IFilePickerService
{
    //选择要导入的照片文件，返回文件的绝对路径列表；用户取消返回空列表
    Task<IReadOnlyList<string>> PickPhotoFilesAsync();

    //选择文件夹（用于首次运行时选定"库根"），返回绝对路径；取消返回 null
    Task<string?> PickFolderAsync(string title);
}
