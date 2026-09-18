namespace PhotoAlbum.Infrastructure.Storage;
using PhotoAlbum.Domain.Interfaces;

//IStorageBackend的本地文件系统实现（哑I/O适配器，只负责"路径 -> 操作"）
//路径规则：相对路径按"库根"解析；绝对路径原样使用（便于导入库外的源文件）
public class FileSystemStorageBackend : IStorageBackend
{
    private readonly string _rootPath;

    //构造方法，传入库根路径
    public FileSystemStorageBackend(string rootPath)
    {
        _rootPath = rootPath;
    }

    //把传入路径解析成真实路径：绝对路径原样用，相对路径拼库根
    private string Resolve(string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(_rootPath, path);

    //创建目录（已存在则忽略）
    public Task CreateDirectoryAsync(string path)
    {
        Directory.CreateDirectory(Resolve(path));
        return Task.CompletedTask;
    }

    //递归删除目录（不存在则忽略）
    public Task DeleteDirectoryAsync(string path)
    {
        string full = Resolve(path);
        if (Directory.Exists(full))
            Directory.Delete(full, recursive: true);
        return Task.CompletedTask;
    }

    //删除文件（不存在则忽略）
    public Task DeleteFileAsync(string path)
    {
        string full = Resolve(path);
        if (File.Exists(full))
            File.Delete(full);
        return Task.CompletedTask;
    }

    //移动文件：先复制到目标，成功后再删源，保证中途失败不丢源
    public async Task MoveFileAsync(string src, string dst)
    {
        string fullSrc = Resolve(src);
        string fullDst = Resolve(dst);

        //确保目标目录存在
        string? dstDir = Path.GetDirectoryName(fullDst);
        if (!string.IsNullOrEmpty(dstDir))
            Directory.CreateDirectory(dstDir);

        //先复制
        using (var srcStream = File.OpenRead(fullSrc))
        using (var dstStream = File.Create(fullDst))
            await srcStream.CopyToAsync(dstStream);

        //复制成功后再删源
        File.Delete(fullSrc);
    }

    //路径是否存在（文件或目录都算）
    public Task<bool> ExistsAsync(string path)
    {
        string full = Resolve(path);
        return Task.FromResult(File.Exists(full) || Directory.Exists(full));
    }
    
    //读取文件流（用于查看原图）
    public Task<Stream> ReadFileAsync(string path)
    {
        Stream stream = File.OpenRead(Resolve(path));
        return Task.FromResult(stream);
    }

    //写入字节（用于缩略图），自动建好父目录
    public async Task WriteBytesAsync(string path, byte[] bytes)
    {
        string full = Resolve(path);
        string? dir = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllBytesAsync(full, bytes);
    }

    //读取字节（用于缩略图缓存）；文件不存在返回null
    public async Task<byte[]?> ReadBytesAsync(string path)
    {
        string full = Resolve(path);
        if (!File.Exists(full))
            return null;
        return await File.ReadAllBytesAsync(full);
    }
}
