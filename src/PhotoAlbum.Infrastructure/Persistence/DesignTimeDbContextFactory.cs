namespace PhotoAlbum.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

//设计时工厂：供 `dotnet ef migrations` 命令行工具创建DbContext用（不参与运行时）
//只负责在设计时也就是执行ef的DB版本git的时候提供当前DB模型状态的任务
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=design.db;Foreign Keys=True")
            .Options;

        return new AppDbContext(options);
    }
}
