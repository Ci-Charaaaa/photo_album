namespace PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Domain.Entities;

public interface IPhotoRepository
{
   
    //CU'R'D操作
    Task CreateAsync(Photo photo);
    Task UpdateAsync(Photo photo);
    Task<Photo?> GetByIdAsync(long photoId);
    Task DeleteAsync(Photo photo);

    //相册和照片的关系的增删操作
    Task AddRelationAsync(PhotoAlbumRelation relation);
    Task RemoveRelationAsync(long photoId, long albumId);

    //全量/某相册/未分类相册 三种情况的分页浏览查询
    Task<IReadOnlyList<Photo>> GetAllAsync(int skip, int take);
    Task<IReadOnlyList<Photo>> GetByAlbumAsync(long albumId, int skip, int take);
    Task<IReadOnlyList<Photo>> GetUnclassifiedAsync(int skip, int take);
}