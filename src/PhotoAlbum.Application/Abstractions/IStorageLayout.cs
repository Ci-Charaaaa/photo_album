namespace PhotoAlbum.Application.Abstractions;

public interface IStorageLayout
{
    //相册目录路径，通过根加祖先id，自己id组合成路径
    string AlbumDirectoryPath(IReadOnlyList<long> ancestorIds);

    //照片文件名，通过照片id，原名，扩展名组合成文件名
    string BuildPhotoFileName(long photoId, string sourceName, string ext);

    //未分类相册目录路径，特殊写死路径
    string UnclassifiedDirectoryPath();

    //照片缩略图路径，通过照片路径拼接thumbnail目录和缩略图文件名组合成路径
    string ThumbnailPath(string photoFilePath);

}