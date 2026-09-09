namespace PhotoAlbum.Application.Services;
using PhotoAlbum.Application.Abstractions;

public class StorageLayout : IStorageLayout
{
    //相册目录路径，通过根加祖先id，自己id组合成路径，注意：虽然参数是祖先id，
    //但实际路径中包含自己id，因为自己id是也相册目录的名称
    public string AlbumDirectoryPath(IReadOnlyList<long> ancestorIds)
    {
        return Path.Combine("albums", string.Join(Path.DirectorySeparatorChar, ancestorIds));
    }

    //照片文件名，通过照片id，原名，扩展名组（拓展名带.，如.jpg）合成文件名
    public string BuildPhotoFileName(long photoId, string sourceName, string ext)
    {
        return $"{photoId}_{sourceName}{ext}";
    }

    //未分类相册目录路径，特殊写死路径
    public string UnclassifiedDirectoryPath()
    {
        return Path.Combine("albums", "unclassified");
    }

    //照片缩略图路径，通过照片路径拼接thumbnail目录和缩略图文件名组合成路径
    public string ThumbnailPath(string photoFilePath)
    {
        return Path.Combine(Path.GetDirectoryName(photoFilePath), "thumbnail", Path.GetFileName(photoFilePath));
    }
}