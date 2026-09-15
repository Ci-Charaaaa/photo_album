namespace PhotoAlbum.Infrastructure.Persistence;

//系统保留的固定id约定
public static class WellKnownIds
{
    //未分类相册的固定id（系统内置，禁止删除，作为主照片的落脚点）
    public const long UnclassifiedAlbumId = 1;
}
