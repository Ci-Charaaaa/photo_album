namespace PhotoAlbum.Application.Services;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Application.Dtos;
using PhotoAlbum.Domain.Entities;
using PhotoAlbum.Domain.Interfaces;

public class PhotoImportService
{
    //创建需要使用的接口的字段
    private readonly IAlbumRepository _albums;
    private readonly IPhotoRepository _photos;
    private readonly IStorageLayout _layout;
    private readonly IStorageBackend _storage;
    private readonly IHashService _hash;
    private readonly IExifService _exif;
    private readonly IThumbnailService _thumbnail;

    //缩略图尺寸，暂定200px
    private const int ThumbnailSize = 200;

    //构造方法，传入依赖
    public PhotoImportService(
        IAlbumRepository albums,
        IPhotoRepository photos,
        IStorageLayout layout,
        IStorageBackend storage,
        IHashService hash,
        IExifService exif,
        IThumbnailService thumbnail)
    {
        _albums = albums;
        _photos = photos;
        _layout = layout;
        _storage = storage;
        _hash = hash;
        _exif = exif;
        _thumbnail = thumbnail;
    }


    //两个用例：查重，导入

    //查重：算内容哈希，返回已存在的照片（null表示没有重复）
    public async Task<Photo?> CheckDuplicateAsync(string sourcePath)
    {
        //哈希是新导入的照片计算得出，因此返回为空才表示没有重复
        string hash = await _hash.ComputeHashAsync(sourcePath);
        return await _photos.GetByHashAsync(hash);
    }

    //导入照片：从源移动(成功才删源)，必入主相册(null则落未分类)，可选附加相册
    public async Task<Photo> ImportAsync(
        string sourcePath,                      //源文件路径
        long? primaryAlbumId,                   //主相册id，null表示落未分类
        IReadOnlyList<long>? extraAlbumIds,     //附加相册id列表，null表示没有附加相册
        bool allowDuplicate)                    //是否允许重复照片
    {
        //算内容哈希（查重和入库都用它）
        string hash = await _hash.ComputeHashAsync(sourcePath);

        //防御性查重：不允许重复且已存在则直接抛错
        if (!allowDuplicate)
        {
            Photo? duplicate = await _photos.GetByHashAsync(hash);
            if (duplicate != null)
                throw new Exception("检测到重复照片。");
        }

        //读EXIF（有则填，无则空）
        ExifData exif = await _exif.ReadAsync(sourcePath);

        //拆出原名和扩展名（扩展名带点，原名不带扩展名）
        string sourceName = Path.GetFileNameWithoutExtension(sourcePath);
        string ext = Path.GetExtension(sourcePath);

        //拿到未分类相册，用于判断落点和作为兜底主相册
        Album? unclassified = await _albums.GetUnclassifiedAsync();
        if (unclassified == null)
            throw new Exception("Unclassified album not found.");

        //确定主相册：传了id就查，没传就落未分类
        Album primaryAlbum;
        if (primaryAlbumId.HasValue)
        {
            Album? found = await _albums.GetByIdAsync(primaryAlbumId.Value);
            if (found == null)
                throw new Exception($"Album with id {primaryAlbumId} not found.");
            primaryAlbum = found;
        }
        else
        {
            primaryAlbum = unclassified;
        }

        //先建照片实体并入库，拿到自增id（文件名要用id，所以必须先写库）
        var photo = new Photo
        {
            Name = sourceName,
            SourceName = sourceName,
            PhotoFilePath = "",
            TakenTime = exif.TakeTime,
            ImportTime = DateTime.Now,
            OtherInfo = exif.OtherInfoJson,
            Hash = hash
        };
        await _photos.CreateAsync(photo);

        //算目标路径：主相册是未分类就用写死的未分类目录，否则用祖先链拼
        string directory = (primaryAlbum.Id == unclassified.Id)
            ? _layout.UnclassifiedDirectoryPath()
            : _layout.AlbumDirectoryPath(await AlbumTree.GetAncestorIdsAsync(_albums, primaryAlbum));
        string fileName = _layout.BuildPhotoFileName(photo.Id, sourceName, ext);
        string targetPath = Path.Combine(directory, fileName);

        //移动源文件到目标路径（后端保证原子：成功才删源）
        await _storage.MoveFileAsync(sourcePath, targetPath);

        //更新照片路径
        photo.PhotoFilePath = targetPath;
        await _photos.UpdateAsync(photo);

        //添加主关系
        await _photos.AddRelationAsync(new PhotoAlbumRelation
        {
            PhotoId = photo.Id,
            AlbumId = primaryAlbum.Id,
            IsPrimary = true
        });

        //添加附加关系(如果有的话)
        if (extraAlbumIds != null)
        {
            foreach (long albumId in extraAlbumIds)
            {
                await _photos.AddRelationAsync(new PhotoAlbumRelation
                {
                    PhotoId = photo.Id,
                    AlbumId = albumId,
                    IsPrimary = false
                });
            }
        }

        //生成缩略图并写盘
        using (var source = await _storage.ReadFileAsync(targetPath))
        {
            byte[] bytes = await _thumbnail.GenerateAsync(source, ThumbnailSize);
            await _storage.WriteBytesAsync(_layout.ThumbnailPath(targetPath), bytes);
        }

        return photo;
    }
}
