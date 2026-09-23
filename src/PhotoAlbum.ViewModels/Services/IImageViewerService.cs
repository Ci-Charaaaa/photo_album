namespace PhotoAlbum.ViewModels.Services;

//原图查看服务的接口
//VM只负责"取到原图字节"，"用窗口显示"是表现层的事，所以这里只留契约
public interface IImageViewerService
{
    //用独立窗口显示原图
    //  title：窗口标题（一般用照片名）
    //  imageData：原图的字节数据
    Task ViewImageAsync(string title, byte[] imageData);
}
