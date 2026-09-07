namespace PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Application.Dtos;

public interface IExifService
{
    //根据路径读取照片的Exif信息，返回ExifData对象
    Task<ExifData> ReadAsync(string path);
}