namespace PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Domain.Entities;

public interface IAlbumRepository
{
    
    //根据父相册id查只读的子相册列表
    Task<IReadOnlyList<Album>> GetChildrenAsync(long parentId);
    //获得未分类相册
    Task<Album?> GetUnclassifiedAsync();
    //CU'R'D操作
    Task CreateAsync(Album album);
    Task UpdateAsync(Album album);
    Task<Album?> GetByIdAsync(long albumId);
    Task DeleteAsync(Album album);

}