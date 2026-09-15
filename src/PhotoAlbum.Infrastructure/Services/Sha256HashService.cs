namespace PhotoAlbum.Infrastructure.Services;
using PhotoAlbum.Application.Abstractions;

//IHashService的实现：用SHA256算文件内容哈希，用于照片查重
public class Sha256HashService : IHashService
{
    public Task<string> ComputeHashAsync(string path)
    {
        throw new NotImplementedException();
    }
}
