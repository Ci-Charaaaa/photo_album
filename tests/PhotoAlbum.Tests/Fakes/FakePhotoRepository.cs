namespace PhotoAlbum.Tests.Fakes;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Domain.Entities;
using PhotoAlbum.Infrastructure.Persistence;

//内存版照片/关系仓储
public class FakePhotoRepository : IPhotoRepository
{
    public List<Photo> Photos { get; } = new();
    public List<PhotoAlbumRelation> Relations { get; } = new();

    public Task<Photo> CreateAsync(Photo photo)
    {
        photo.Id = Photos.Count == 0 ? 1 : Photos.Max(p => p.Id) + 1;
        Photos.Add(photo);
        return Task.FromResult(photo);
    }

    public Task UpdateAsync(Photo photo) => Task.CompletedTask;   // 引用类型，内存中已是同一对象

    public Task<Photo?> GetByIdAsync(long photoId)
        => Task.FromResult(Photos.FirstOrDefault(p => p.Id == photoId));

    public Task DeleteAsync(Photo photo)
    {
        Photos.RemoveAll(p => p.Id == photo.Id);
        return Task.CompletedTask;
    }

    public Task AddRelationAsync(PhotoAlbumRelation relation)
    {
        Relations.Add(relation);
        return Task.CompletedTask;
    }

    public Task RemoveRelationAsync(long photoId, long albumId)
    {
        Relations.RemoveAll(r => r.PhotoId == photoId && r.AlbumId == albumId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PhotoAlbumRelation>> GetRelationsByAlbumAsync(long albumId)
        => Task.FromResult<IReadOnlyList<PhotoAlbumRelation>>(Relations.Where(r => r.AlbumId == albumId).ToList());

    public Task<IReadOnlyList<PhotoAlbumRelation>> GetRelationsByPhotoAsync(long photoId)
        => Task.FromResult<IReadOnlyList<PhotoAlbumRelation>>(Relations.Where(r => r.PhotoId == photoId).ToList());

    public Task<Photo?> GetByHashAsync(string hash)
        => Task.FromResult(Photos.FirstOrDefault(p => p.Hash == hash));

    public Task ChangePrimaryAlbumAsync(long photoId, long newAlbumId)
    {
        PhotoAlbumRelation? old = Relations.FirstOrDefault(r => r.PhotoId == photoId && r.IsPrimary);
        if (old != null)
            Relations.Remove(old);

        PhotoAlbumRelation? target = Relations.FirstOrDefault(r => r.PhotoId == photoId && r.AlbumId == newAlbumId);
        if (target != null)
            target.IsPrimary = true;
        else
            Relations.Add(new PhotoAlbumRelation { PhotoId = photoId, AlbumId = newAlbumId, IsPrimary = true });

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Photo>> GetAllAsync(int skip, int take)
        => Task.FromResult<IReadOnlyList<Photo>>(
            Photos.OrderByDescending(p => p.ImportTime).Skip(skip).Take(take).ToList());

    public Task<IReadOnlyList<Photo>> GetByAlbumAsync(long albumId, int skip, int take)
        => Task.FromResult<IReadOnlyList<Photo>>(
            Photos.Where(p => Relations.Any(r => r.PhotoId == p.Id && r.AlbumId == albumId))
                  .OrderByDescending(p => p.ImportTime).Skip(skip).Take(take).ToList());

    public Task<IReadOnlyList<Photo>> GetUnclassifiedAsync(int skip, int take)
        => Task.FromResult<IReadOnlyList<Photo>>(
            Photos.Where(p => Relations.Any(r => r.PhotoId == p.Id && r.AlbumId == WellKnownIds.UnclassifiedAlbumId && r.IsPrimary))
                  .OrderByDescending(p => p.ImportTime).Skip(skip).Take(take).ToList());
}
