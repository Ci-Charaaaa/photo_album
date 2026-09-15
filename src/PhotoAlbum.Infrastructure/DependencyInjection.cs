namespace PhotoAlbum.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

//基础设施层的依赖注册入口：把各接口映射到本层的具体实现
public static class DependencyInjection
{
    //注册DbContext、仓储、存储后端、缩略图/EXIF/哈希服务
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        //TODO: 逐项注册接口 -> 实现
        throw new NotImplementedException();
    }
}
