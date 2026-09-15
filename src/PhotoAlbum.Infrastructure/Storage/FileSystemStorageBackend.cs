namespace PhotoAlbum.Infrastructure.Storage;
using PhotoAlbum.Domain.Interfaces;

//IStorageBackend的本地文件系统实现（哑I/O适配器，只负责路径->操作）
public class FileSystemStorageBackend : IStorageBackend
{
    public Task CreateDirectoryAsync(string path)
    {
        throw new NotImplementedException();
    }

    public Task DeleteDirectoryAsync(string path)
    {
        throw new NotImplementedException();
    }

    public Task DeleteFileAsync(string path)
    {
        throw new NotImplementedException();
    }

    public Task MoveFileAsync(string src, string dst)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(string path)
    {
        throw new NotImplementedException();
    }

    public Task<Stream> ReadFileAsync(string path)
    {
        throw new NotImplementedException();
    }

    public Task WriteBytesAsync(string path, byte[] bytes)
    {
        throw new NotImplementedException();
    }

    public Task<byte[]> ReadBytesAsync(string path)
    {
        throw new NotImplementedException();
    }
}
