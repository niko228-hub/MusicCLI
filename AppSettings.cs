using System.Text.Json;

namespace MusicCli;

public sealed class AppSettings
{
    public string? Folder { get; set; }
    public string? SelectedFile { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string FilePath
    {
        get
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(root))
                root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");

            return Path.Combine(root, "MusicCli", "settings.json");
        }
    }

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new AppSettings();

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
        }
    }

    public void Remember(string folder, string? selectedFile)
    {
        Folder = folder;
        SelectedFile = selectedFile;
        Save();
    }
}