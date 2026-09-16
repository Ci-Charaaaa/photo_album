namespace PhotoAlbum.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Domain.Entities;

//IPhotoRepository的EF Core实现，管理照片表和关系表
public class EfPhotoRepository : IPhotoRepository
{
    private readonly AppDbContext _db;

    //构造方法，传入DbContext
    public EfPhotoRepository(AppDbContext db)
    {
        _db = db;
    }

    //创建照片并入库，返回带自增id的实体
    public async Task<Photo> CreateAsync(Photo photo)
    {
        _db.Photos.Add(photo);
        await _db.SaveChangesAsync();
        return photo;
    }

    //更新照片
    public async Task UpdateAsync(Photo photo)
    {
        _db.Photos.Update(photo);
        await _db.SaveChangesAsync();
    }

    //根据id查照片
    public async Task<Photo?> GetByIdAsync(long photoId)
    {
        return await _db.Photos
            .FirstOrDefaultAsync(p => p.Id == photoId);
    }

    //删除照片记录
    public async Task DeleteAsync(Photo photo)
    {
        _db.Photos.Remove(photo);
        await _db.SaveChangesAsync();
    }

    //新增一条照片-相册关系
    public async Task AddRelationAsync(PhotoAlbumRelation relation)
    {
        _db.PhotoAlbumRelations.Add(relation);
        await _db.SaveChangesAsync();
    }

    //删除一条照片-相册关系，没有则忽略
    public async Task RemoveRelationAsync(long photoId, long albumId)
    {
        var relation = await _db.PhotoAlbumRelations
            .FirstOrDefaultAsync(r => r.PhotoId == photoId && r.AlbumId == albumId);
        if (relation == null)
            return;

        _db.PhotoAlbumRelations.Remove(relation);
        await _db.SaveChangesAsync();
    }

    //查某相册下的所有关系
    public async Task<IReadOnlyList<PhotoAlbumRelation>> GetRelationsByAlbumAsync(long albumId)
    {
        return await _db.PhotoAlbumRelations
            .Where(r => r.AlbumId == albumId)
            .ToListAsync();
    }

    //查某照片的所有关系
    public async Task<IReadOnlyList<PhotoAlbumRelation>> GetRelationsByPhotoAsync(long photoId)
    {
        return await _db.PhotoAlbumRelations
            .Where(r => r.PhotoId == photoId)
            .ToListAsync();
    }

    //按hash查照片（查重）
    public async Task<Photo?> GetByHashAsync(string hash)
    {
        return await _db.Photos
            .FirstOrDefaultAsync(p => p.Hash == hash);
    }

    //把照片的主相册改指到newAlbumId：删旧主关系，再把目标关系升级为或新增为主关系
    public async Task ChangePrimaryAlbumAsync(long photoId, long newAlbumId)
    {
        //找到当前的主关系
        var oldPrimary = await _db.PhotoAlbumRelations
            .FirstOrDefaultAsync(r => r.PhotoId == photoId && r.IsPrimary);

        //已经是目标主相册就无需处理
        if (oldPrimary != null && oldPrimary.AlbumId == newAlbumId)
            return;

        //删掉旧主关系（不直接改组合主键，改为删旧建新，避免改键问题）
        if (oldPrimary != null)
            _db.PhotoAlbumRelations.Remove(oldPrimary);

        //防御性处理：目标相册已有关系（正常流程不会出现）则复用，否则新增一条主关系
        var target = await _db.PhotoAlbumRelations
            .FirstOrDefaultAsync(r => r.PhotoId == photoId && r.AlbumId == newAlbumId);
        if (target != null)
            target.IsPrimary = true;
        else
            _db.PhotoAlbumRelations.Add(new PhotoAlbumRelation
            {
                PhotoId = photoId,
                AlbumId = newAlbumId,
                IsPrimary = true
            });

        await _db.SaveChangesAsync();
    }

    //默认相册：全量浏览（按导入时间倒序分页）
    public async Task<IReadOnlyList<Photo>> GetAllAsync(int skip, int take)
    {
        return await _db.Photos
            .AsNoTracking()
            .OrderByDescending(p => p.ImportTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    //相册内浏览：含主/附加关系（按导入时间倒序分页）
    public async Task<IReadOnlyList<Photo>> GetByAlbumAsync(long albumId, int skip, int take)
    {
        return await _db.Photos
            .AsNoTracking()
            .Where(p => p.PhotoAlbumRelations.Any(r => r.AlbumId == albumId))
            .OrderByDescending(p => p.ImportTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    //未分类浏览：主关系指向未分类的照片（按导入时间倒序分页）
    public async Task<IReadOnlyList<Photo>> GetUnclassifiedAsync(int skip, int take)
    {
        return await _db.Photos
            .AsNoTracking()
            .Where(p => p.PhotoAlbumRelations.Any(r => r.AlbumId == WellKnownIds.UnclassifiedAlbumId && r.IsPrimary))
            .OrderByDescending(p => p.ImportTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
}
