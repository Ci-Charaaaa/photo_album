namespace PhotoAlbum.Application.Abstractions;

public interface IHashService
{
    //比较哈希对照片查重
    Task<string> ComputeHashAsync(string path);
}