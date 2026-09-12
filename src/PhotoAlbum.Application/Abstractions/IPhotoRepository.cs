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

    //查某相册下的所有关系（用于删除级联时逐条读IsPrimary）
    Task<IReadOnlyList<PhotoAlbumRelation>> GetRelationsByAlbumAsync(long albumId);

    //把照片的主相册从原来的改指到newAlbumId（包含未分类相册）
    Task ChangePrimaryAlbumAsync(long photoId, long newAlbumId);

    //全量/某相册/未分类相册 三种情况的分页浏览查询
    Task<IReadOnlyList<Photo>> GetAllAsync(int skip, int take);
    Task<IReadOnlyList<Photo>> GetByAlbumAsync(long albumId, int skip, int take);
    Task<IReadOnlyList<Photo>> GetUnclassifiedAsync(int skip, int take);
}