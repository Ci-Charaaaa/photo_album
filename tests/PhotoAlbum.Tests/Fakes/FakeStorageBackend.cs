namespace PhotoAlbum.Tests.Fakes;
using PhotoAlbum.Domain.Interfaces;

//内存版存储后端：用字典模拟文件系统，供Application层测试使用
public class FakeStorageBackend : IStorageBackend
{
    // 规范化后的路径 -> 文件内容
    public Dictionary<string, byte[]> Files { get; } = new();
    // 目录集合
    public HashSet<string> Directories { get; } = new();

    //统一分隔符，避免Windows/Linux差异
    public static string Normalize(string path) => path.Replace('\\', '/').TrimEnd('/');

    public Task CreateDirectoryAsync(string path)
    {
        Directories.Add(Normalize(path));
        return Task.CompletedTask;
    }

    public Task DeleteDirectoryAsync(string path)
    {
        string dir = Normalize(path);
        Directories.RemoveWhere(d => d == dir || d.StartsWith(dir + "/"));
        foreach (string key in Files.Keys.Where(k => k == dir || k.StartsWith(dir + "/")).ToList())
            Files.Remove(key);
        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(string path)
    {
        Files.Remove(Normalize(path));
        return Task.CompletedTask;
    }

    //移动=复制到目标后删源（模拟真实后端的原子移动）
    public Task MoveFileAsync(string src, string dst)
    {
        string s = Normalize(src);
        string d = Normalize(dst);
        if (!Files.TryGetValue(s, out byte[]? bytes))
            throw new FileNotFoundException($"Source not found: {s}");

        Files[d] = bytes;
        Files.Remove(s);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string path)
    {
        string p = Normalize(path);
        return Task.FromResult(Files.ContainsKey(p) || Directories.Contains(p));
    }

    public Task<Stream> ReadFileAsync(string path)
    {
        string p = Normalize(path);
        if (!Files.TryGetValue(p, out byte[]? bytes))
            throw new FileNotFoundException($"File not found: {p}");

        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public Task WriteBytesAsync(string path, byte[] bytes)
    {
        Files[Normalize(path)] = bytes;
        return Task.CompletedTask;
    }

    public Task<byte[]?> ReadBytesAsync(string path)
    {
        return Task.FromResult(Files.TryGetValue(Normalize(path), out byte[]? bytes) ? bytes : null);
    }

    //测试辅助：某后缀的文件是否存在
    public bool HasFileEndingWith(string suffix) => Files.Keys.Any(k => k.EndsWith(suffix));
}
