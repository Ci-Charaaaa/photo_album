namespace PhotoAlbum.Infrastructure.Services;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Application.Dtos;

//IExifService的MetadataExtractor实现：读取照片的EXIF信息
public class MetadataExtractorExifService : IExifService
{
    public Task<ExifData> ReadAsync(string path)
    {
        throw new NotImplementedException();
    }
}
