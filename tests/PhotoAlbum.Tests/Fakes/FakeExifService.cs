namespace PhotoAlbum.Tests.Fakes;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Application.Dtos;

//内存版EXIF服务：返回预设结果
public class FakeExifService : IExifService
{
    public ExifData Result { get; set; } = new ExifData();

    public Task<ExifData> ReadAsync(string path) => Task.FromResult(Result);
}
