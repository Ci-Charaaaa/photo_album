namespace PhotoAlbum.Application.Services;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Domain.Entities;

//相册树相关的共享工具类，供多个服务复用
//祖先链遍历属于用例编排逻辑，不是领域不变量也不是具体技术，所以放应用层
public static class AlbumTree
{
    //获取相册的祖先id列表，包括自己id，返回一个从根到叶子的id列表
    //albums作为参数传入，避免这里再持有依赖，也省去各服务的构造函数改动
    public static async Task<List<long>> GetAncestorIdsAsync(IAlbumRepository albums, Album album)
    {
        var ids = new List<long>();

        //沿着ParentId逐级向上，先得到"自己->根"的逆序
        long? parentId = album.ParentId;
        while (parentId != null)
        {
            Album? parent = await albums.GetByIdAsync(parentId.Value);
            if (parent == null)
                //父相册查不到属于数据异常，直接抛出，也避免while死循环
                throw new Exception($"Parent album with id {parentId} not found.");

            ids.Add(parent.Id);
            parentId = parent.ParentId;
        }

        //反转成"根->父"，再补上自己，得到"根->自己"
        ids.Reverse();
        ids.Add(album.Id);
        return ids;
    }
}
