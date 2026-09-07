namespace PhotoAlbum.Application.Dtos;

public class ExifData
{
    //Exif的数据体只包含时间和其他两个部分
    public DateTime? TakeTime { get; init; }
    public string? OtherInfoJson { get; init; }
}