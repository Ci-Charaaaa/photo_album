namespace PhotoAlbum.Domain.Entities;

public class Album
{
    // 基础属性，id，名称，父级id，备注，封面照片id，后三者可以为空
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public long? ParentId { get; set; }
    public string? Remark { get; set; }
    public long? CoverPhotoId { get; set; }

    // 关联属性，父级相册，子级相册集合，封面照片
    public Album? Parent { get; set; }
    public ICollection<Album> Children { get; set; } = [];
    public Photo? CoverPhoto { get; set; }





}