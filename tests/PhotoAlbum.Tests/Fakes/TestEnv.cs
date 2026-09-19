namespace PhotoAlbum.Tests.Fakes;
using PhotoAlbum.Application.Services;
using PhotoAlbum.Domain.Entities;
using PhotoAlbum.Infrastructure.Persistence;

//测试环境：用假实现 + 真实StorageLayout组装出被测服务
public class TestEnv
{
    public FakeAlbumRepository Albums { get; } = new();
    public FakePhotoRepository Photos { get; } = new();
    public FakeStorageBackend Storage { get; } = new();
    public StorageLayout Layout { get; } = new();
    public FakeHashService Hash { get; } = new();
    public FakeExifService Exif { get; } = new();
    public FakeThumbnailService Thumbnail { get; } = new();

    public AlbumService AlbumService => new(Albums, Photos, Layout, Storage);
    public PhotoImportService ImportService => new(Albums, Photos, Layout, Storage, Hash, Exif, Thumbnail);
    public PhotoService PhotoService => new(Photos, Layout, Storage, Thumbnail);

    //种入未分类相册（Id=1）
    public Task<Album> SeedUnclassifiedAsync()
        => Albums.CreateAsync(new Album { Name = "未分类" });

    //种入一个相册
    public Task<Album> SeedAlbumAsync(string name, long? parentId = null)
        => Albums.CreateAsync(new Album { Name = name, ParentId = parentId });

    //种入一张照片：建记录 + 关系 + 落一个文件
    public async Task<Photo> SeedPhotoAsync(
        Album album, string fileName, bool primary,
        DateTime? importTime = null, string? hash = null, bool withThumbnail = false)
    {
        bool isUnclassified = album.Id == WellKnownIds.UnclassifiedAlbumId;
        List<long> chain = await GetChainAsync(album);
        string dir = isUnclassified
            ? Layout.UnclassifiedDirectoryPath()
            : Layout.AlbumDirectoryPath(chain);
        string path = Path.Combine(dir, fileName);

        Photo photo = await Photos.CreateAsync(new Photo
        {
            Name = Path.GetFileNameWithoutExtension(fileName),
            SourceName = Path.GetFileNameWithoutExtension(fileName),
            PhotoFilePath = path,
            Hash = hash ?? ("H_" + fileName),
            ImportTime = importTime ?? DateTime.Now,
        });

        await Photos.AddRelationAsync(new PhotoAlbumRelation
        {
            PhotoId = photo.Id,
            AlbumId = album.Id,
            IsPrimary = primary
        });

        await Storage.WriteBytesAsync(path, new byte[] { 1, 2, 3 });
        if (withThumbnail)
            await Storage.WriteBytesAsync(Layout.ThumbnailPath(path), new byte[] { 4, 5 });
        return photo;
    }

    //求相册"根->自己"的id链（与AlbumTree逻辑一致）
    private async Task<List<long>> GetChainAsync(Album album)
    {
        var ids = new List<long>();
        long? cur = album.ParentId;
        while (cur != null)
        {
            Album? parent = await Albums.GetByIdAsync(cur.Value);
            if (parent == null)
                break;
            ids.Add(parent.Id);
            cur = parent.ParentId;
        }
        ids.Reverse();
        ids.Add(album.Id);
        return ids;
    }
}
