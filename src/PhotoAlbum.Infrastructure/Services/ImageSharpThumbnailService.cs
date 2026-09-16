namespace PhotoAlbum.Infrastructure.Services;
using PhotoAlbum.Application.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

//IThumbnailService的ImageSharp实现：把原图压成指定尺寸的缩略图字节
public class ImageSharpThumbnailService : IThumbnailService
{
    //读入原图，按最长边缩放到size（保持宽高比），输出与原图同格式的字节
    public async Task<byte[]> GenerateAsync(Stream source, int size)
    {
        using var image = await Image.LoadAsync(source);

        //按最长边缩放，Mode=Max 表示不放大超出、等比缩到能塞进 size×size
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(size, size),
            Mode = ResizeMode.Max
        }));

        //用原图格式编码，识别不出格式就用JPEG
        var format = image.Metadata.DecodedImageFormat ?? JpegFormat.Instance;

        using var ms = new MemoryStream();
        await image.SaveAsync(ms, format);
        return ms.ToArray();
    }
}
