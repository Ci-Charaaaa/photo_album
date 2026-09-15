namespace PhotoAlbum.Infrastructure.Persistence;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Domain.Entities;

//IAlbumRepository的EF Core实现
public class EfAlbumRepository : IAlbumRepository
{
    private readonly AppDbContext _db;

    public EfAlbumRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<Album>> GetChildrenAsync(long parentId)
    {
        throw new NotImplementedException();
    }

    public Task<Album?> GetUnclassifiedAsync()
    {
        throw new NotImplementedException();
    }

    public Task<Album> CreateAsync(Album album)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Album album)
    {
        throw new NotImplementedException();
    }

    public Task<Album?> GetByIdAsync(long albumId)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Album album)
    {
        throw new NotImplementedException();
    }
}
