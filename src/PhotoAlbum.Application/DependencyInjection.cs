namespace PhotoAlbum.Application;
using Microsoft.Extensions.DependencyInjection;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Application.Services;

//应用层的依赖注册入口：用例服务 + 本层的实现（StorageLayout是纯计算，实现留本层）
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        //本层的实现：路径布局服务
        services.AddScoped<IStorageLayout, StorageLayout>();

        //三个用例服务
        services.AddScoped<AlbumService>();
        services.AddScoped<PhotoImportService>();
        services.AddScoped<PhotoService>();

        return services;
    }
}
