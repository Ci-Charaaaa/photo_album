namespace PhotoAlbum.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Domain.Entities;

//IAlbumRepository的EF Core实现，所有数据读写都通过注入的AppDbContext
public class EfAlbumRepository : IAlbumRepository
{
    private readonly AppDbContext _db;

    //构造方法，传入DbContext
    public EfAlbumRepository(AppDbContext db)
    {
        _db = db;
    }

    //根据父相册id查子相册
    public async Task<IReadOnlyList<Album>> GetChildrenAsync(long parentId)
    {
        return await _db.Albums
            .Where(a => a.ParentId == parentId)
            .ToListAsync();
    }

    //获取未分类相册（按约定的固定id查）
    public async Task<Album?> GetUnclassifiedAsync()
    {
        return await _db.Albums
            .FirstOrDefaultAsync(a => a.Id == WellKnownIds.UnclassifiedAlbumId);
    }

    //创建相册并入库，返回带自增id的实体
    public async Task<Album> CreateAsync(Album album)
    {
        _db.Albums.Add(album);
        await _db.SaveChangesAsync();
        return album;
    }

    //更新相册
    public async Task UpdateAsync(Album album)
    {
        _db.Albums.Update(album);
        await _db.SaveChangesAsync();
    }

    //根据id查相册
    public async Task<Album?> GetByIdAsync(long albumId)
    {
        return await _db.Albums
            .FirstOrDefaultAsync(a => a.Id == albumId);
    }

    //删除相册记录
    public async Task DeleteAsync(Album album)
    {
        _db.Albums.Remove(album);
        await _db.SaveChangesAsync();
    }
}
