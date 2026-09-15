namespace PhotoAlbum.Infrastructure.Persistence;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Domain.Entities;

//IPhotoRepository的EF Core实现
public class EfPhotoRepository : IPhotoRepository
{
    private readonly AppDbContext _db;

    public EfPhotoRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Photo> CreateAsync(Photo photo)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Photo photo)
    {
        throw new NotImplementedException();
    }

    public Task<Photo?> GetByIdAsync(long photoId)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Photo photo)
    {
        throw new NotImplementedException();
    }

    public Task AddRelationAsync(PhotoAlbumRelation relation)
    {
        throw new NotImplementedException();
    }

    public Task RemoveRelationAsync(long photoId, long albumId)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<PhotoAlbumRelation>> GetRelationsByAlbumAsync(long albumId)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<PhotoAlbumRelation>> GetRelationsByPhotoAsync(long photoId)
    {
        throw new NotImplementedException();
    }

    public Task<Photo?> GetByHashAsync(string hash)
    {
        throw new NotImplementedException();
    }

    public Task ChangePrimaryAlbumAsync(long photoId, long newAlbumId)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Photo>> GetAllAsync(int skip, int take)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Photo>> GetByAlbumAsync(long albumId, int skip, int take)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Photo>> GetUnclassifiedAsync(int skip, int take)
    {
        throw new NotImplementedException();
    }
}
