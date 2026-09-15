namespace PhotoAlbum.Infrastructure.Services;
using PhotoAlbum.Application.Abstractions;

//IThumbnailService的ImageSharp实现：把原图压成指定尺寸的缩略图字节
public class ImageSharpThumbnailService : IThumbnailService
{
    public Task<byte[]> GenerateAsync(Stream source, int size)
    {
        throw new NotImplementedException();
    }
}
