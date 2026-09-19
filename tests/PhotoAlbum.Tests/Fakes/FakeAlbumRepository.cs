namespace PhotoAlbum.Tests.Fakes;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Domain.Entities;
using PhotoAlbum.Infrastructure.Persistence;

//内存版相册仓储
public class FakeAlbumRepository : IAlbumRepository
{
    public List<Album> Albums { get; } = new();

    public Task<IReadOnlyList<Album>> GetAllAsync()
        => Task.FromResult<IReadOnlyList<Album>>(Albums.ToList());

    public Task<Album?> GetByIdAsync(long albumId)
        => Task.FromResult(Albums.FirstOrDefault(a => a.Id == albumId));

    public Task<IReadOnlyList<Album>> GetChildrenAsync(long parentId)
        => Task.FromResult<IReadOnlyList<Album>>(Albums.Where(a => a.ParentId == parentId).ToList());

    public Task<Album?> GetUnclassifiedAsync()
        => Task.FromResult(Albums.FirstOrDefault(a => a.Id == WellKnownIds.UnclassifiedAlbumId));

    public Task<Album> CreateAsync(Album album)
    {
        album.Id = Albums.Count == 0 ? 1 : Albums.Max(a => a.Id) + 1;
        Albums.Add(album);
        return Task.FromResult(album);
    }

    public Task UpdateAsync(Album album)
    {
        // 引用类型，内存中已是同一对象，直接改过了
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Album album)
    {
        Albums.RemoveAll(a => a.Id == album.Id);
        return Task.CompletedTask;
    }
}
