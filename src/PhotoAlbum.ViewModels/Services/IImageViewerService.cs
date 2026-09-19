namespace PhotoAlbum.ViewModels.Services;

//原图查看服务（由表现层实现，VM只面向接口）
public interface IImageViewerService
{
    //用独立窗口显示原图
    Task ViewImageAsync(string title, byte[] imageData);
}
