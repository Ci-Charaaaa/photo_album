namespace PhotoAlbum.Application.Services;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Domain.Entities;
using PhotoAlbum.Domain.Interfaces;

public class PhotoService
{
    //创建需要使用的接口的字段
    private readonly IPhotoRepository _photos;
    private readonly IAlbumRepository _albums;
    private readonly IStorageLayout _layout;
    private readonly IStorageBackend _storage;
    private readonly IThumbnailService _thumbnail;

    //缩略图尺寸
    private const int ThumbnailSize = 200;

    //构造方法，传入依赖
    public PhotoService(IPhotoRepository photos, IAlbumRepository albums, IStorageLayout layout, IStorageBackend storage, IThumbnailService thumbnail)
    {
        _photos = photos;
        _albums = albums;
        _layout = layout;
        _storage = storage;
        _thumbnail = thumbnail;
    }

    //用例：浏览照片，取缩略图，看原图，删除照片，移动照片，重命名照片，改备注

    //浏览：按视图（全量/某相册/未分类）分页查询照片
    public Task<IReadOnlyList<Photo>> GetPhotosAsync(PhotoView view, long? albumId, int skip, int take)
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

    //查看原图：按id读出原图字节（供查看器窗口使用）
    public async Task<byte[]> GetOriginalBytesAsync(long photoId)
    {
        Photo? photo = await _photos.GetByIdAsync(photoId);
        if (photo == null)
            throw new Exception($"Photo with id {photoId} not found.");

        using var stream = await _storage.ReadFileAsync(photo.PhotoFilePath);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
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

    //移动照片到另一个相册（改主相册）：物理文件+缩略图跟随移动，路径更新，主关系改指
    public async Task MovePhotoAsync(long photoId, long newAlbumId)
    {
        Photo? photo = await _photos.GetByIdAsync(photoId);
        if (photo == null)
            throw new Exception($"Photo with id {photoId} not found.");

        Album? target = await _albums.GetByIdAsync(newAlbumId);
        if (target == null)
            throw new Exception($"Album with id {newAlbumId} not found.");

        Album? unclassified = await _albums.GetUnclassifiedAsync();

        //算目标目录：未分类用写死目录，否则用祖先链拼
        bool isUnclassified = unclassified != null && target.Id == unclassified.Id;
        string directory = isUnclassified
            ? _layout.UnclassifiedDirectoryPath()
            : _layout.AlbumDirectoryPath(await AlbumTree.GetAncestorIdsAsync(_albums, target));

        //文件名不变，只换目录
        string fileName = Path.GetFileName(photo.PhotoFilePath);
        string newPath = Path.Combine(directory, fileName);

        //已在目标目录则只需保证主关系（正常不会发生）
        if (newPath == photo.PhotoFilePath)
        {
            await _photos.ChangePrimaryAlbumAsync(photoId, newAlbumId);
            return;
        }

        //先移物理文件（原图 + 缩略图，缩略图可能不存在）
        await _storage.MoveFileAsync(photo.PhotoFilePath, newPath);
        string oldThumbnail = _layout.ThumbnailPath(photo.PhotoFilePath);
        if (await _storage.ExistsAsync(oldThumbnail))
            await _storage.MoveFileAsync(oldThumbnail, _layout.ThumbnailPath(newPath));

        //再更新路径与主相册
        photo.PhotoFilePath = newPath;
        await _photos.UpdateAsync(photo);
        await _photos.ChangePrimaryAlbumAsync(photoId, newAlbumId);
    }

    //重命名照片：只改显示名，磁盘文件名不动（磁盘名以source_name为准）
    public async Task RenamePhotoAsync(long photoId, string newName)
    {
        Photo? photo = await _photos.GetByIdAsync(photoId);
        if (photo == null)
            throw new Exception($"Photo with id {photoId} not found.");

        photo.Name = newName;
        await _photos.UpdateAsync(photo);
    }

    //修改照片备注：只改库
    public async Task UpdatePhotoRemarkAsync(long photoId, string? remark)
    {
        Photo? photo = await _photos.GetByIdAsync(photoId);
        if (photo == null)
            throw new Exception($"Photo with id {photoId} not found.");

        photo.Remark = remark;
        await _photos.UpdateAsync(photo);
    }
}
