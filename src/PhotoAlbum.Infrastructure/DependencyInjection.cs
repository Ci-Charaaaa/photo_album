namespace PhotoAlbum.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Domain.Interfaces;
using PhotoAlbum.Infrastructure.Persistence;
using PhotoAlbum.Infrastructure.Services;
using PhotoAlbum.Infrastructure.Storage;

//基础设施层的依赖注册入口：把各接口映射到本层的具体实现
public static class DependencyInjection
{
    //根据"库根"配置数据库和文件后端
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string libraryRoot)
    {
        //库根不存在则创建
        Directory.CreateDirectory(libraryRoot);

        //数据库文件放在库根下；Foreign Keys=True 确保SQLite外键约束开启（含级联）
        string dbPath = Path.Combine(libraryRoot, "photoalbum.db");
        string connectionString = $"Data Source={dbPath};Foreign Keys=True";

        //数据库上下文（默认Scoped：一次操作一个）
        services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(connectionString));

        //仓储
        services.AddScoped<IAlbumRepository, EfAlbumRepository>();
        services.AddScoped<IPhotoRepository, EfPhotoRepository>();

        //文件后端（带库根）
        services.AddScoped<IStorageBackend>(_ => new FileSystemStorageBackend(libraryRoot));

        //外部服务（无状态，可单例）
        services.AddSingleton<IHashService, Sha256HashService>();
        services.AddSingleton<IExifService, MetadataExtractorExifService>();
        services.AddSingleton<IThumbnailService, ImageSharpThumbnailService>();

        return services;
    }

    //应用迁移，把数据库结构升级到最新（首次运行会建库建表并写入种子数据）
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}
