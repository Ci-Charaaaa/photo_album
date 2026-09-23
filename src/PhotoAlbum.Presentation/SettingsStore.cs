namespace PhotoAlbum.Presentation;
using System.Text.Json;

//用户设置的读写（目前只保存"库根"路径），存到用户 AppData 目录：
//  Windows: C:\Users\<用户>\AppData\Roaming\PhotoAlbum\settings.json
//选择"丙方案"后，首启让用户选库根，之后每次启动从这里读，不再询问。
public class SettingsStore
{
    private readonly string _path;   // settings.json 的完整路径

    //设置的数据结构（JSON 反序列化用）。私有嵌套类，仅本文件用。
    private sealed class Settings
    {
        public string? LibraryRoot { get; set; }
    }

    public SettingsStore()
    {
        //%AppData%\PhotoAlbum\settings.json
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PhotoAlbum");
        Directory.CreateDirectory(dir);   // 目录不存在就建
        _path = Path.Combine(dir, "settings.json");
    }

    //读取库根；没设置过 / 文件损坏 / 读失败 都返回 null（调用方据此决定是否让用户选）
    public string? LoadLibraryRoot()
    {
        try
        {
            if (!File.Exists(_path))
                return null;

            string json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<Settings>(json)?.LibraryRoot;
        }
        catch
        {
            return null;
        }
    }

    //保存库根（写成 JSON）
    public void SaveLibraryRoot(string root)
    {
        string json = JsonSerializer.Serialize(new Settings { LibraryRoot = root });
        File.WriteAllText(_path, json);
    }
}
