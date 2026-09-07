namespace PhotoAlbum.Application.Abstractions;

public interface IThumbnailService
{
    //拿到原图的文件流和缩略图的尺寸，返回压缩后的缩略图的字节数组
    Task<byte[]> GenerateAsync(Stream source, int size);
}