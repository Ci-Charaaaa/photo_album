namespace PhotoAlbum.Application.Services;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Domain.Entities;
using PhotoAlbum.Domain.Interfaces;

public class PhotoService
{
    //创建需要使用的接口的字段
    private readonly IPhotoRepository _photos;
    private readonly IStorageLayout _layout;
    private readonly IStorageBackend _storage;
    private readonly IThumbnailService _thumbnail;

    //缩略图尺寸，暂定200px
    private const int ThumbnailSize = 200;

    //构造方法，传入依赖
    public PhotoService(IPhotoRepository photos, IStorageLayout layout, IStorageBackend storage, IThumbnailService thumbnail)
    {
        _photos = photos;
        _layout = layout;
        _storage = storage;
        _thumbnail = thumbnail;
    }


    //四个用例：浏览照片，取缩略图，看原图，删除照片

    //浏览：按视图（全量/某相册/未分类）分页查询照片
    public Task<IReadOnlyList<Photo>> GetPhotosAsync(
        PhotoView view,     //视图类型，枚举三种
        long? albumId,      //相册id，只有view=Album时才有值
        int skip,           //跳过多少
        int take)           //取多少
    {
        //根据视图分派到仓储对应的查询方法
        return view switch
        {
            PhotoView.All => _photos.GetAllAsync(skip, take),
            PhotoView.Unclassified => _photos.GetUnclassifiedAsync(skip, take),
            PhotoView.Album when albumId.HasValue => _photos.GetByAlbumAsync(albumId.Value, skip, take),
            _ => throw new Exception("Invalid photo view or missing album id.")
        };
    }

    //取缩略图：先读磁盘缓存，未命中就生成并写盘
    public async Task<byte[]> GetThumbnailAsync(Photo photo)
    {
        string thumbPath = _layout.ThumbnailPath(photo.PhotoFilePath);

        //先尝试读缓存（约定：文件不存在时返回null）
        byte[]? cached = await _storage.ReadBytesAsync(thumbPath);
        if (cached != null)
            return cached;

        //未命中：读原图 -> 生成缩略图 -> 写盘 -> 返回
        using (var source = await _storage.ReadFileAsync(photo.PhotoFilePath))
        {
            byte[] bytes = await _thumbnail.GenerateAsync(source, ThumbnailSize);
            await _storage.WriteBytesAsync(thumbPath, bytes);
            return bytes;
        }
    }

    //查看原图：读出原图流
    public Task<Stream> GetOriginalAsync(Photo photo)
    {
        return _storage.ReadFileAsync(photo.PhotoFilePath);
    }

    //删除照片：删原图、删缩略图、删关系、删记录
    public async Task DeletePhotoAsync(long photoId)
    {
        Photo? photo = await _photos.GetByIdAsync(photoId);
        if (photo == null)
            throw new Exception($"Photo with id {photoId} not found.");

        //先删物理文件（原图 + 缩略图）
        await _storage.DeleteFileAsync(photo.PhotoFilePath);
        await _storage.DeleteFileAsync(_layout.ThumbnailPath(photo.PhotoFilePath));

        //再删该照片的所有关系边
        var relations = await _photos.GetRelationsByPhotoAsync(photoId);
        foreach (var relation in relations)
            await _photos.RemoveRelationAsync(relation.PhotoId, relation.AlbumId);

        //最后删照片记录
        await _photos.DeleteAsync(photo);
    }
}
