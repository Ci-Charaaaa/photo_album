namespace PhotoAlbum.Tests.Fakes;
using PhotoAlbum.Application.Abstractions;

//内存版缩略图服务：返回预设字节
public class FakeThumbnailService : IThumbnailService
{
    public byte[] Data { get; set; } = new byte[] { 9, 9, 9 };

    public Task<byte[]> GenerateAsync(Stream source, int size) => Task.FromResult(Data);
}
