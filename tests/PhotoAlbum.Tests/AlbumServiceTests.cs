namespace PhotoAlbum.Tests;
using PhotoAlbum.Domain.Entities;
using PhotoAlbum.Tests.Fakes;

//测试范围：第九部分"删除相册用例(3)" + AC-1 相册CRUD(3)
public class AlbumServiceTests
{
    //AC-1.2 创建多级相册：记录与磁盘目录同步
    [Fact]
    public async Task CreateAlbum_CreatesRecordAndDirectory()
    {
        var env = new TestEnv();
        await env.SeedUnclassifiedAsync();

        var parent = await env.AlbumService.CreateAlbumAsync("邦邦", null);
        var child = await env.AlbumService.CreateAlbumAsync("mygo", parent.Id);

        Assert.Equal(parent.Id, child.ParentId);
        var children = await env.Albums.GetChildrenAsync(parent.Id);
        Assert.Contains(children, a => a.Id == child.Id);

        // 父目录、子目录都已创建（规范化后以"/"分隔）
        Assert.Contains(env.Storage.Directories, d => d.EndsWith($"albums/{parent.Id}"));
        Assert.Contains(env.Storage.Directories, d => d.EndsWith($"albums/{parent.Id}/{child.Id}"));
    }

    //AC-1.3 改名：只改库，磁盘不动
    [Fact]
    public async Task RenameAlbum_UpdatesNameOnly()
    {
        var env = new TestEnv();
        await env.SeedUnclassifiedAsync();
        var album = await env.AlbumService.CreateAlbumAsync("邦邦", null);
        string dirsBefore = string.Join(",", env.Storage.Directories.OrderBy(d => d));

        await env.AlbumService.RenameAlbumAsync(album.Id, "bangbang");

        var updated = await env.Albums.GetByIdAsync(album.Id);
        Assert.Equal("bangbang", updated!.Name);
        // 目录集合不变
        Assert.Equal(dirsBefore, string.Join(",", env.Storage.Directories.OrderBy(d => d)));
    }

    //AC-1.1 删除空相册：无副作用
    [Fact]
    public async Task DeleteEmptyAlbum_RemovesRecordAndDirectory()
    {
        var env = new TestEnv();
        await env.SeedUnclassifiedAsync();
        var album = await env.AlbumService.CreateAlbumAsync("空相册", null);

        await env.AlbumService.DeleteAlbumAsync(album.Id);

        Assert.Null(await env.Albums.GetByIdAsync(album.Id));
        Assert.DoesNotContain(env.Storage.Directories, d => d.EndsWith($"albums/{album.Id}"));
        Assert.Empty(env.Photos.Photos);
        Assert.Empty(env.Photos.Relations);
    }

    //删除用例1：无子相册、只含主照片 → 照片回落未分类
    [Fact]
    public async Task DeleteAlbum_PrimaryPhotoFallsBackToUnclassified()
    {
        var env = new TestEnv();
        var unclassified = await env.SeedUnclassifiedAsync();
        var album = await env.SeedAlbumAsync("A");
        var photo = await env.SeedPhotoAsync(album, "p.jpg", primary: true, withThumbnail: true);

        await env.AlbumService.DeleteAlbumAsync(album.Id);

        // 相册删除、照片保留
        Assert.Null(await env.Albums.GetByIdAsync(album.Id));
        Assert.NotNull(await env.Photos.GetByIdAsync(photo.Id));

        // 关系改指未分类且仍是主关系
        var rel = Assert.Single(await env.Photos.GetRelationsByPhotoAsync(photo.Id));
        Assert.Equal(unclassified.Id, rel.AlbumId);
        Assert.True(rel.IsPrimary);

        // 文件（原图 + 缩略图）移动到未分类目录
        Assert.True(env.Storage.HasFileEndingWith("unclassified/p.jpg"));
        Assert.True(env.Storage.HasFileEndingWith("unclassified/thumbnail/p.jpg"));
        Assert.False(env.Storage.HasFileEndingWith($"albums/{album.Id}/p.jpg"));
    }

    //删除用例2：含子相册、子相册只含主照片 → 级联删除，照片回落未分类
    [Fact]
    public async Task DeleteAlbum_WithChildren_CascadeDeletes()
    {
        var env = new TestEnv();
        var unclassified = await env.SeedUnclassifiedAsync();
        var root = await env.SeedAlbumAsync("根");
        var child = await env.SeedAlbumAsync("子", root.Id);
        var photo = await env.SeedPhotoAsync(child, "p.jpg", primary: true);

        await env.AlbumService.DeleteAlbumAsync(root.Id);

        Assert.Null(await env.Albums.GetByIdAsync(root.Id));
        Assert.Null(await env.Albums.GetByIdAsync(child.Id));
        Assert.NotNull(await env.Photos.GetByIdAsync(photo.Id));
        Assert.True(env.Storage.HasFileEndingWith("unclassified/p.jpg"));

        var rel = Assert.Single(await env.Photos.GetRelationsByPhotoAsync(photo.Id));
        Assert.Equal(unclassified.Id, rel.AlbumId);
    }

    //删除用例3：同时含主照片和附加照片 → 主回落未分类，附加只断关系
    [Fact]
    public async Task DeleteAlbum_AdditionalPhotoOnlyUnlinked()
    {
        var env = new TestEnv();
        var unclassified = await env.SeedUnclassifiedAsync();
        var a = await env.SeedAlbumAsync("A");
        var b = await env.SeedAlbumAsync("B");

        var p1 = await env.SeedPhotoAsync(a, "p1.jpg", primary: true);   // A的主照片
        var p2 = await env.SeedPhotoAsync(b, "p2.jpg", primary: true);   // B的主照片
        // p2 附加到 A
        await env.Photos.AddRelationAsync(new PhotoAlbumRelation { PhotoId = p2.Id, AlbumId = a.Id, IsPrimary = false });

        await env.AlbumService.DeleteAlbumAsync(a.Id);

        // p1 主照片回落未分类
        var rel1 = Assert.Single(await env.Photos.GetRelationsByPhotoAsync(p1.Id));
        Assert.Equal(unclassified.Id, rel1.AlbumId);
        Assert.True(env.Storage.HasFileEndingWith("unclassified/p1.jpg"));

        // p2 与A的关系被删除，仍属于B，文件不动
        var rels2 = await env.Photos.GetRelationsByPhotoAsync(p2.Id);
        Assert.Single(rels2);
        Assert.Equal(b.Id, rels2[0].AlbumId);
        Assert.True(env.Storage.HasFileEndingWith($"albums/{b.Id}/p2.jpg"));
    }
}
