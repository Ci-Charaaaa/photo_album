namespace PhotoAlbum.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PhotoAlbum.Domain.Entities;

//应用数据库上下文，负责实体与数据库表的映射（EF Core）
public class AppDbContext : DbContext
{
    //四张表：相册，照片，人物，关联
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<PhotoAlbumRelation> PhotoAlbumRelations => Set<PhotoAlbumRelation>();
    public DbSet<Person> Persons => Set<Person>();

    //构造方法，传入DbContext配置
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    //配置表名、主键、外键、关系等映射规则
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //相册表：用自引用实现树结构；封面照片可空
        modelBuilder.Entity<Album>(e =>
        {
            //此处是lambda在C#中的第二种写法，直接把lambda翻译成表达式树
            e.ToTable("photo_set");
            e.HasKey(a => a.Id);
            e.Property(a => a.Name).IsRequired();

            //父相册自引用（ParentId指向同表的Id），Restrict避免误级联删除
            e.HasOne(a => a.Parent)
             .WithMany(a => a.Children)
             .HasForeignKey(a => a.ParentId)
             .OnDelete(DeleteBehavior.Restrict);

            //封面照片（可空），照片被删时把封面置空
            e.HasOne(a => a.CoverPhoto)
             .WithMany()
             .HasForeignKey(a => a.CoverPhotoId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        //照片表：hash建索引供查重；显示名/原名/路径/哈希必填
        modelBuilder.Entity<Photo>(e =>
        {
            e.ToTable("photo");
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired();
            e.Property(p => p.SourceName).IsRequired();
            e.Property(p => p.PhotoFilePath).IsRequired();
            e.Property(p => p.Hash).IsRequired();
            e.HasIndex(p => p.Hash);
        });

        //关联表：组合主键(PhotoId,AlbumId)；两个外键分别指向照片和相册
        modelBuilder.Entity<PhotoAlbumRelation>(e =>
        {
            e.ToTable("photo_album");
            e.HasKey(r => new { r.PhotoId, r.AlbumId });

            e.HasOne(r => r.Photo)
             .WithMany(p => p.PhotoAlbumRelations)
             .HasForeignKey(r => r.PhotoId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Album)
             .WithMany()
             .HasForeignKey(r => r.AlbumId)
             .OnDelete(DeleteBehavior.Cascade);

            //按相册查关系较常用，单独加索引
            e.HasIndex(r => r.AlbumId);
        });

        //人物表：阶段二预留，暂不参与业务
        modelBuilder.Entity<Person>(e =>
        {
            e.ToTable("person");
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired();
        });

        //未分类相册：系统内置，固定id，保证始终存在作为主照片的落脚点
        modelBuilder.Entity<Album>().HasData(new Album
        {
            Id = WellKnownIds.UnclassifiedAlbumId,
            Name = "未分类"
        });

        base.OnModelCreating(modelBuilder);
    }
}
