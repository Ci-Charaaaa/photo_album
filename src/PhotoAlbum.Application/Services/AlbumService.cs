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

    //获取相册的祖先id列表，包括自己id，返回一个从根到叶子的id列表
    private async Task<List<long>> GetAlbumAnceIdsAsync(Album album)
    {
        List<long> ancestorIds = new List<long>();
        long? parentId = album.ParentId;
        while (parentId != null)
        {
            Album? parentAlbum = await _albums.GetByIdAsync(parentId.Value);
            if (parentAlbum != null)
            {
                ancestorIds.Add(parentAlbum.Id);
                parentId = parentAlbum.ParentId;
            }
            else
            {
                throw new Exception($"Parent album with id {parentId} not found.");
            }
        }
        //将祖先id列表反转，使其从根到叶子排列，并添加当前相册id
        ancestorIds.Reverse();
        ancestorIds.Add(album.Id);
        return ancestorIds;
    }

    //根据用户输入的名字和（可能存在的）当前相册创建相册的用例
    public async Task<Album> CreateAlbumAsync(string name, long? parentId) 
    {
        //先创建一个相册实体
        Album album = await _albums.CreateAsync(new Album { Name = name, ParentId = parentId });

        //创建路径，父id列表的变量，获取相册的祖先id列表，包括自己id
        string path = "";
        List<long> ancestorIds = await GetAlbumAnceIdsAsync(album);;


        //利用父id列表计算路径
        path = _layout.AlbumDirectoryPath(ancestorIds);

        //传路径创建dir
        await _storage.CreateDirectoryAsync(path);

        //若发现创建失败则手动删除DB里的脏数据
        if(!await _storage.ExistsAsync(path))
        {
            await _albums.DeleteAsync(album);
            throw new Exception($"Failed to create album directory at {path}. Album creation rolled back.");
        }
        else
            return album;

    }

    //根据id查相册，查到直接更新名字，查不到抛异常
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

    //删除相册，先查找相册，查不到抛异常，查到则递归删除子相册，
    //再删除照片关系，最后删除相册
    public async Task DeleteAlbumAsync(long albumId) 
    {
        //查找相册，查不到抛异常
        Album? album = await _albums.GetByIdAsync(albumId);
        if (album == null)
            throw new Exception($"Album with id {albumId} not found.");

        var result = new List<(Album Album,List<long> Chain)>();

        //获取相册及其所有子孙相册的列表，返回一个包含相册和其祖先id链的元组列表
        await CollectSubtreeAsync(album, result);

        var unclassified = await _albums.GetUnclassifiedAsync();
        if(unclassified == null)
            throw new Exception("Unclassified album not found.");

        foreach (var (subAlbum, chain) in result)
        {
            //调取照片处理方法
            await DetachPhotosAsync(
                subAlbum,
                unclassified,
                _layout.UnclassifiedDirectoryPath());
        }

        //逆向删除相册及其子孙相册，先删除子孙相册，再删除父相册
        for (int i = result.Count - 1; i >= 0; i--)
        {
            //删除子孙相册的目录，且一并清除缩略图
            await _storage.DeleteDirectoryAsync(_layout.AlbumDirectoryPath(result[i].Chain));
            //删除子孙相册的DB记录
            await _albums.DeleteAsync(result[i].Album);
        }
            

    }

    //获取相册及其所有子孙相册的列表，返回一个包含相册和其祖先id链的元组列表
    private async Task CollectSubtreeAsync(
        Album album,                                                      
        List<(Album Album, List<long> Chain)> result)
    {
        //添加自己和祖先id链到结果列表中
        var chain = await GetAlbumAnceIdsAsync(album);   
        result.Add((album, chain));

        //递归获取子相册
        var children = await _albums.GetChildrenAsync(album.Id);
        foreach (var child in children)
            await CollectSubtreeAsync(child, result);    
    }

    //对相册内主照片进行转移到未分类相册的操作，传入当前相册，
    //未分类相册，以及未分类相册的路径
    private async Task DetachPhotosAsync(
        Album album, 
        Album unclassified, 
        string unclassifiedDir)
    {
        long albumId = album.Id;

        //先调取照片接口的关系查找方法，递归删除照片关系，
        //若是主相册则转移到未分类相册，非主相册直接删除关系，最后删除相册
        var rel = await _photos.GetRelationsByAlbumAsync(albumId);
        foreach (PhotoAlbumRelation relation in rel)
        {
            if (relation.IsPrimary)
            {
                //获得相册路径和照片名称
                var photo = await _photos.GetByIdAsync(relation.PhotoId);
                if (photo == null)
                    throw new Exception($"Photo with id {relation.PhotoId} not found.");

                string photoFileName = Path.GetFileName(photo.PhotoFilePath);

                //物理移动文件(照片和缩略图)到未分类目录
                await _storage.MoveFileAsync(photo.PhotoFilePath, Path.Combine(unclassifiedDir, Path.GetFileName(photo.PhotoFilePath)));
                await _storage.MoveFileAsync(Path.Combine(_layout.ThumbnailPath(photo.PhotoFilePath)), Path.Combine(unclassifiedDir, "thumbnail", Path.GetFileName(photo.PhotoFilePath)));

                //DB上更改照片的主相册指向未分类相册
                await _photos.ChangePrimaryAlbumAsync(relation.PhotoId, unclassified.Id);

                //更新照片的DB
                photo.PhotoFilePath = Path.Combine(unclassifiedDir, Path.GetFileName(photo.PhotoFilePath));
                await _photos.UpdateAsync(photo);
            }
            else
            {
                //删除非主相册的关系
                await _photos.RemoveRelationAsync(relation.PhotoId, albumId);
            }
        }

    }

}