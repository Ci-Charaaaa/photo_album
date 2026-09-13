namespace PhotoAlbum.Application.Services; 
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Domain.Entities;
using PhotoAlbum.Domain.Interfaces;

public class AlbumService
{
    //创建需要使用的接口的字段
    private readonly IAlbumRepository _albums;
    private readonly IPhotoRepository _photos;
    private readonly IStorageLayout _layout;
    private readonly IStorageBackend _storage;

    //构造函数，传入依赖
    public AlbumService(IAlbumRepository albums, IPhotoRepository photos, IStorageLayout layout, IStorageBackend storage)
    {
        _albums = albums;
        _photos = photos;
        _layout = layout;
        _storage = storage;
    }

    //四个用例：创建相册，重命名相册，修改备注，删除相册

    //根据用户输入的名字和（可能存在的）当前父相册创建相册
    public async Task<Album> CreateAlbumAsync(string name, long? parentId) 
    {
        //先创建一个相册实体并入库，拿到自增id（目录以id命名，所以必须先写库）
        Album album = await _albums.CreateAsync(new Album { Name = name, ParentId = parentId });

        //获取相册的祖先id列表，包括自己id，再算出目录路径
        List<long> ancestorIds = await AlbumTree.GetAncestorIdsAsync(_albums, album);
        string path = _layout.AlbumDirectoryPath(ancestorIds);

        //传路径创建dir
        await _storage.CreateDirectoryAsync(path);

        //若创建失败则手动删除DB里的脏数据（DB和文件系统不在同一事务）
        if (!await _storage.ExistsAsync(path))
        {
            await _albums.DeleteAsync(album);
            throw new Exception($"Failed to create album directory at {path}. Album creation rolled back.");
        }

        return album;
    }

    //根据id查相册，查到直接更新名字，查不到抛异常（目录以id命名，改名不动磁盘）
    public async Task RenameAlbumAsync(long albumId, string newName) 
    {
        Album? album = await _albums.GetByIdAsync(albumId);
        if (album == null)
            throw new Exception($"Album with id {albumId} not found.");
        album.Name = newName;
        await _albums.UpdateAsync(album);
    }

    //同上，只是名字换成备注
    public async Task UpdateAlbumRemarkAsync(long albumId, string remark) 
    {
        Album? album = await _albums.GetByIdAsync(albumId);
        if (album == null)
            throw new Exception($"Album with id {albumId} not found.");
        album.Remark = remark;
        await _albums.UpdateAsync(album);
    }

    //删除相册，先查找相册，查不到抛异常，查到则级联删除整棵子树
    public async Task DeleteAlbumAsync(long albumId) 
    {
        //查找相册，查不到抛异常
        Album? album = await _albums.GetByIdAsync(albumId);
        if (album == null)
            throw new Exception($"Album with id {albumId} not found.");

        //拿到未分类相册作为主照片的落脚点
        Album? unclassified = await _albums.GetUnclassifiedAsync();
        if (unclassified == null)
            throw new Exception("Unclassified album not found.");

        //获取相册及其所有子孙相册的列表，每个元素是"相册 + 自己的祖先id链"
        var result = new List<(Album Album, List<long> Chain)>();
        await CollectSubtreeAsync(album, result);

        //处理每个相册里的照片：主照片回落未分类，附加照片只断关系
        foreach (var (subAlbum, chain) in result)
        {
            await DetachPhotosAsync(
                subAlbum,
                unclassified,
                _layout.UnclassifiedDirectoryPath());
        }

        //逆向删除目录和DB记录，先删子孙相册，再删父相册（自底向上）
        for (int i = result.Count - 1; i >= 0; i--)
        {
            //删除相册的目录，且一并清除缩略图
            await _storage.DeleteDirectoryAsync(_layout.AlbumDirectoryPath(result[i].Chain));
            //删除相册的DB记录
            await _albums.DeleteAsync(result[i].Album);
        }
    }



    //两个私有的辅助方法：收集子孙相册，处理相册内照片

    //递归获取相册及其所有子孙相册的列表，返回一个包含相册和其祖先id链的元组列表
    private async Task CollectSubtreeAsync(
        Album album,
        List<(Album Album, List<long> Chain)> result)
    {
        //添加自己和祖先id链到结果列表中
        var chain = await AlbumTree.GetAncestorIdsAsync(_albums, album);
        result.Add((album, chain));

        //递归获取子相册
        var children = await _albums.GetChildrenAsync(album.Id);
        foreach (var child in children)
            await CollectSubtreeAsync(child, result);
    }

    //对相册内照片进行分流：主照片移到未分类，附加照片只删关系
    private async Task DetachPhotosAsync(
        Album album, 
        Album unclassified, 
        string unclassifiedDir)
    {
        long albumId = album.Id;

        //先调取照片接口的关系查找方法
        var rel = await _photos.GetRelationsByAlbumAsync(albumId);
        foreach (PhotoAlbumRelation relation in rel)
        {
            if (relation.IsPrimary)
            {
                //拿到照片本体，查不到就跳过这条脏关系
                var photo = await _photos.GetByIdAsync(relation.PhotoId);
                if (photo == null)
                    continue;

                //文件名不变，只换目录：复用原路径里的文件名
                string fileName = Path.GetFileName(photo.PhotoFilePath);
                string newPath = Path.Combine(unclassifiedDir, fileName);

                //物理移动文件(照片和缩略图)到未分类目录，成功后才动DB
                await _storage.MoveFileAsync(photo.PhotoFilePath, newPath);
                await _storage.MoveFileAsync(_layout.ThumbnailPath(photo.PhotoFilePath), _layout.ThumbnailPath(newPath));

                //更新照片路径，并把主相册改指未分类
                photo.PhotoFilePath = newPath;
                await _photos.UpdateAsync(photo);
                await _photos.ChangePrimaryAlbumAsync(photo.Id, unclassified.Id);
            }
            else
            {
                //附加照片：只删除关系，物理文件不动
                await _photos.RemoveRelationAsync(relation.PhotoId, albumId);
            }
        }
    }
}
