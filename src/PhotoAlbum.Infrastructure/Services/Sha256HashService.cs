namespace PhotoAlbum.Infrastructure.Services;
using System.Security.Cryptography;
using PhotoAlbum.Application.Abstractions;

//IHashService的实现：用SHA256算文件内容哈希，用于照片查重
//SHA2556:SHA-2家族的医院，输入任意长度的字节，输出256位（32字节）哈希值，
//2的256次幂决定其碰撞概率极低
//但是其在拿到哈希反推原文时的计算速度相对较快，
//所以不适合密码存储（因为容易被GPU破解），但用于文件查重是可以的
public class Sha256HashService : IHashService
{
    //读取文件内容并算出SHA256哈希（返回十六进制字符串）
    public async Task<string> ComputeHashAsync(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        byte[] hash = await sha.ComputeHashAsync(stream);
        return Convert.ToHexString(hash);
    }
}
