namespace PhotoAlbum.Domain.Entities;


public class PhotoAlbumRelation
{
    // 基础属性，相册id，照片id，是否为主照片 
    public long AlbumId { get; set; }
    public long PhotoId { get; set; }
    public bool IsPrimary { get; set; }

    // 关联属性，相册，照片，且不能为空
    public Album Album { get; set; } = null!;
    public Photo Photo { get; set; } = null!;


}