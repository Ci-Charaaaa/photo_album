namespace PhotoAlbum.Infrastructure.Services;
using System.Text.Json;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using PhotoAlbum.Application.Abstractions;
using PhotoAlbum.Application.Dtos;

//IExifService的MetadataExtractor实现：读取照片的EXIF信息
public class MetadataExtractorExifService : IExifService
{
    //读取EXIF：拍摄时间单独拆出，其余冷门字段打包成JSON（有则读，无则留空）
    public Task<ExifData> ReadAsync(string path)
    {
        DateTime? takeTime = null;
        string? otherJson = null;

        try
        {
            //注意：MetadataExtractor.Directory 与 System.IO.Directory 同名，这里全限定
            IReadOnlyList<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(path);

            //拍摄时间：从Exif SubIFD里取
            var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (subIfd != null && subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dt))
                takeTime = dt;

            //其余字段打包
            otherJson = BuildOtherInfoJson(directories);
        }
        catch
        {
            //读不到就留空，不让EXIF问题影响导入
        }

        return Task.FromResult(new ExifData { TakeTime = takeTime, OtherInfoJson = otherJson });
    }

    //把所有目录的标签打包成一个"目录名 -> {标签名: 值}"的嵌套JSON
    private static string? BuildOtherInfoJson(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        //此处的dir是元数据分组（Directory），每个dir里有若干标签（Tag），
        //每个标签有Name和Description，此处对元数据的列表挨个拆解拿exif内部的信息，
        //最终返回一个JSON字符串
        var map = new Dictionary<string, Dictionary<string, string>>();

        foreach (var directory in directories)
        {
            var tags = new Dictionary<string, string>();
            foreach (var tag in directory.Tags)
            {
                if (string.IsNullOrEmpty(tag.Description))
                    continue;
                tags[tag.Name] = tag.Description;
            }

            if (tags.Count > 0)
                map[directory.Name] = tags;
        }

        return map.Count == 0 ? null : JsonSerializer.Serialize(map);
    }
}
