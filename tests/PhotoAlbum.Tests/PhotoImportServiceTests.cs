namespace PhotoAlbum.Tests;
using PhotoAlbum.Application.Dtos;
using PhotoAlbum.Tests.Fakes;

//测试范围：AC-2 导入功能(3) + 查重/未分类(2)
public class PhotoImportServiceTests
{
    //AC-2.1 导入：源文件移动到目标目录，创建主关系
    [Fact]
    public async Task Import_MovesFileAndCreatesPrimaryRelation()
    {
        var env = new TestEnv();
        await env.SeedUnclassifiedAsync();
        var album = await env.SeedAlbumAsync("A");
        await env.Storage.WriteBytesAsync("source/x.jpg", new byte[] { 1, 2 });

        var photo = await env.ImportService.ImportAsync("source/x.jpg", album.Id, null, false);

        // 源文件被移走
        Assert.False(env.Storage.Files.ContainsKey("source/x.jpg"));
        // 目标落在相册目录下，文件名 = {id}_{原名}{扩展名}
        Assert.True(env.Storage.HasFileEndingWith($"albums/{album.Id}/{photo.Id}_x.jpg"));
        // 主关系
        var rel = Assert.Single(await env.Photos.GetRelationsByPhotoAsync(photo.Id));
        Assert.Equal(album.Id, rel.AlbumId);
        Assert.True(rel.IsPrimary);
    }

    //导入未指定相册 → 落未分类
    [Fact]
    public async Task Import_WhenNoAlbum_UsesUnclassified()
    {
        var env = new TestEnv();
        var unclassified = await env.SeedUnclassifiedAsync();
        await env.Storage.WriteBytesAsync("source/y.jpg", new byte[] { 1 });

        var photo = await env.ImportService.ImportAsync("source/y.jpg", null, null, false);

        Assert.True(env.Storage.HasFileEndingWith($"unclassified/{photo.Id}_y.jpg"));
        var rel = Assert.Single(await env.Photos.GetRelationsByPhotoAsync(photo.Id));
        Assert.Equal(unclassified.Id, rel.AlbumId);
    }

    //AC-2.2 查重：返回已存在的照片
    [Fact]
    public async Task CheckDuplicate_ReturnsExisting()
    {
        var env = new TestEnv();
        await env.SeedUnclassifiedAsync();
        await env.Storage.WriteBytesAsync("source/x.jpg", new byte[] { 1 });

        var photo = await env.ImportService.ImportAsync("source/x.jpg", null, null, false);
        var duplicate = await env.ImportService.CheckDuplicateAsync("source/x.jpg");

        Assert.NotNull(duplicate);
        Assert.Equal(photo.Id, duplicate!.Id);
    }

    //重复导入且不允许重复 → 抛异常
    [Fact]
    public async Task Import_Duplicate_WithoutAllow_Throws()
    {
        var env = new TestEnv();
        await env.SeedUnclassifiedAsync();
        env.Hash.HashFactory = _ => "SAME";   // 强制两文件内容哈希相同
        await env.Storage.WriteBytesAsync("source/x.jpg", new byte[] { 1 });
        await env.Storage.WriteBytesAsync("source/y.jpg", new byte[] { 1 });

        await env.ImportService.ImportAsync("source/x.jpg", null, null, false);

        await Assert.ThrowsAsync<Exception>(() =>
            env.ImportService.ImportAsync("source/y.jpg", null, null, false));
    }

    //AC-2.3 EXIF：拍摄时间写入照片
    [Fact]
    public async Task Import_StoresExifTakeTime()
    {
        var env = new TestEnv();
        await env.SeedUnclassifiedAsync();
        var taken = new DateTime(2020, 5, 1, 10, 0, 0);
        env.Exif.Result = new ExifData { TakeTime = taken };
        await env.Storage.WriteBytesAsync("source/z.jpg", new byte[] { 1 });

        var photo = await env.ImportService.ImportAsync("source/z.jpg", null, null, false);

        Assert.Equal(taken, photo.TakenTime);
    }
}
