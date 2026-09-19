namespace PhotoAlbum.Presentation;
using System.Text.Json;

//读写用户设置（目前只存"库根"），放在用户AppData目录下
public class SettingsStore
{
    private readonly string _path;

    //设置内容
    private sealed class Settings
    {
        public string? LibraryRoot { get; set; }
    }

    public SettingsStore()
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PhotoAlbum");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "settings.json");
    }

    //读取库根，没设置过或读取失败返回null
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

    //保存库根
    public void SaveLibraryRoot(string root)
    {
        string json = JsonSerializer.Serialize(new Settings { LibraryRoot = root });
        File.WriteAllText(_path, json);
    }
}
