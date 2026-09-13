namespace PhotoAlbum.Application.Services;

//浏览照片的视图类型：默认相册(全量)、相册内、未分类
public enum PhotoView
{
    //默认相册：全量视图，显示所有照片
    All,
    //相册内：某个相册下的照片
    Album,
    //未分类：主相册指向未分类的照片
    Unclassified
}
