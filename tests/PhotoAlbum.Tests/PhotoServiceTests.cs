namespace PhotoAlbum.Tests;
using PhotoAlbum.Application.Services;
using PhotoAlbum.Tests.Fakes;

//测试范围：AC-3 视图/删除(3) + 缩略图缓存(1)
public class PhotoServiceTests
{
    //AC-3.1 默认视图：显示全部照片，按导入时间倒序
    [Fact]
    public async Task GetPhotos_All_OrdersByImportTimeDesc()
    {
        var env = new TestEnv();
        var u = await env.SeedUnclassifiedAsync();
        var oldPhoto = await env.SeedPhotoAsync(u, "old.jpg", true, new DateTime(2020, 1, 1));
        var newPhoto = await env.SeedPhotoAsync(u, "new.jpg", true, new DateTime(2021, 1, 1));

        var list = await env.PhotoService.GetPhotosAsync(PhotoView.All, null, 0, 10);

        Assert.Equal(2, list.Count);
        Assert.Equal(newPhoto.Id, list[0].Id);   // 最新在前
        Assert.Equal(oldPhoto.Id, list[1].Id);
    }

    //AC-3.2 未分类视图：只显示主关系指向未分类的照片
    [Fact]
    public async Task GetPhotos_Unclassified_OnlyUnclassifiedPrimary()
    {
        var env = new TestEnv();
        var u = await env.SeedUnclassifiedAsync();
        var other = await env.SeedAlbumAsync("A");
        var inUnclassified = await env.SeedPhotoAsync(u, "a.jpg", true);
        await env.SeedPhotoAsync(other, "b.jpg", true);

        var list = await env.PhotoService.GetPhotosAsync(PhotoView.Unclassified, null, 0, 10);

        Assert.Single(list);
        Assert.Equal(inUnclassified.Id, list[0].Id);
    }

    //AC-3.3 删除照片：文件、缩略图、关系、记录都删
    [Fact]
    public async Task DeletePhoto_RemovesFileThumbnailRelationsAndRecord()
    {
        var env = new TestEnv();
        var u = await env.SeedUnclassifiedAsync();
        var photo = await env.SeedPhotoAsync(u, "p.jpg", true);
        await env.Storage.WriteBytesAsync(env.Layout.ThumbnailPath(photo.PhotoFilePath), new byte[] { 7 });

        await env.PhotoService.DeletePhotoAsync(photo.Id);

        Assert.Null(await env.Photos.GetByIdAsync(photo.Id));
        Assert.Empty(await env.Photos.GetRelationsByPhotoAsync(photo.Id));
        Assert.False(env.Storage.HasFileEndingWith("unclassified/p.jpg"));       // 原图删除
        Assert.False(env.Storage.HasFileEndingWith("thumbnail/p.jpg"));          // 缩略图删除
    }

    //缩略图：缓存未命中 → 生成并写盘；命中 → 直接读缓存
    [Fact]
    public async Task GetThumbnail_GeneratesAndCachesOnMiss()
    {
        var env = new TestEnv();
        var u = await env.SeedUnclassifiedAsync();
        var photo = await env.SeedPhotoAsync(u, "p.jpg", true);

        // 首次：未命中 → 生成 + 写盘
        env.Thumbnail.Data = new byte[] { 42 };
        var first = await env.PhotoService.GetThumbnailAsync(photo);
        Assert.Equal(new byte[] { 42 }, first);
        Assert.True(env.Storage.HasFileEndingWith("thumbnail/p.jpg"));

        // 二次：命中缓存（即使生成器改了数据，也应返回缓存的旧数据）
        env.Thumbnail.Data = new byte[] { 99 };
        var second = await env.PhotoService.GetThumbnailAsync(photo);
        Assert.Equal(new byte[] { 42 }, second);
    }

    //移动照片：文件+缩略图跟随移动，主关系改指目标相册
    [Fact]
    public async Task MovePhoto_MovesFileAndChangesPrimary()
    {
        var env = new TestEnv();
        await env.SeedUnclassifiedAsync();
        var a = await env.SeedAlbumAsync("A");
        var b = await env.SeedAlbumAsync("B");
        var photo = await env.SeedPhotoAsync(a, "p.jpg", primary: true, withThumbnail: true);

        await env.PhotoService.MovePhotoAsync(photo.Id, b.Id);

        // 主关系改指B
        var rel = Assert.Single(await env.Photos.GetRelationsByPhotoAsync(photo.Id));
        Assert.Equal(b.Id, rel.AlbumId);
        Assert.True(rel.IsPrimary);

        // 文件（原图+缩略图）移动到B目录，A目录下不再有
        Assert.True(env.Storage.HasFileEndingWith($"albums/{b.Id}/p.jpg"));
        Assert.True(env.Storage.HasFileEndingWith($"albums/{b.Id}/thumbnail/p.jpg"));
        Assert.False(env.Storage.HasFileEndingWith($"albums/{a.Id}/p.jpg"));
    }

    //重命名照片：只改显示名，磁盘文件名不变
    [Fact]
    public async Task RenamePhoto_UpdatesNameOnly()
    {
        var env = new TestEnv();
        var u = await env.SeedUnclassifiedAsync();
        var photo = await env.SeedPhotoAsync(u, "p.jpg", primary: true);

        await env.PhotoService.RenamePhotoAsync(photo.Id, "新名字");

        var updated = await env.Photos.GetByIdAsync(photo.Id);
        Assert.Equal("新名字", updated!.Name);
        Assert.True(env.Storage.HasFileEndingWith("unclassified/p.jpg"));
    }

    //修改照片备注
    [Fact]
    public async Task UpdatePhotoRemark_UpdatesRemark()
    {
        var env = new TestEnv();
        var u = await env.SeedUnclassifiedAsync();
        var photo = await env.SeedPhotoAsync(u, "p.jpg", primary: true);

        await env.PhotoService.UpdatePhotoRemarkAsync(photo.Id, "这是备注");

        var updated = await env.Photos.GetByIdAsync(photo.Id);
        Assert.Equal("这是备注", updated!.Remark);
    }
}
