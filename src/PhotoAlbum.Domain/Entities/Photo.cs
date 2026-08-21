namespace PhotoAlbum.Domain.Entities;

public class Photo
{
    // 基础属性，id，名称，备注，来源名称，照片文件路径，
    // 拍摄时间，导入时间，其他信息，哈希值，其中备注，拍摄时间，
	// 其他信息可以为空
    public long Id { get; set; }
	public string Name { get; set; } = null!;
	public string? Remark { get; set; }
	public string SourceName { get; set; } = null!;
    public string PhotoFilePath { get; set; } = null!;
    public DateTime? TakenTime { get; set; }
	public DateTime ImportTime { get; set; } 
	public string? OtherInfo{ get; set; }
	public string Hash{ get; set; } = null!;

    // 关联属性，照片相册关系集合
    public ICollection<PhotoAlbumRelation> PhotoAlbumRelations { get; set; } = [];






}
