namespace PhotoAlbum.Domain.Interfaces;


public interface IStorageBackend
{
    // 基础方法，目录创建，目录删除，
    // 文件删除，文件移动，读取文件流（用于照片读），
    // 文件是否存在，写入字节流，读取字节流（两个字节流用于缩略图读写）
    public Task CreateDirectoryAsync(string path);
    public Task DeleteDirectoryAsync(string path);
    public Task DeleteFileAsync(string path);
    public Task MoveFileAsync(string src,string dst);
    public Task<bool> ExistsAsync(string path);
    public Task<Stream> ReadFileAsync(string path);
    public Task WriteBytesAsync(string path, byte[] bytes);
    public Task<byte[]> ReadBytesAsync(string path);
    



       







}