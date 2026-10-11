namespace MusicCli.Player;

public sealed class TrackLibrary
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".ogg", ".flac", ".m4a", ".aac", ".wma"
    };

    public string Folder { get; private set; }
    public IReadOnlyList<string> Tracks { get; private set; } = Array.Empty<string>();
    public int SelectedIndex { get; private set; } = -1;

    public TrackLibrary(string folder)
    {
        Folder = Path.GetFullPath(folder);
        Rescan();
    }

    public string? SelectedTrack =>
        SelectedIndex >= 0 && SelectedIndex < Tracks.Count
            ? Tracks[SelectedIndex]
            : null;

    public string? SelectedName =>
        SelectedTrack is null ? null : Path.GetFileName(SelectedTrack);

    public bool ChangeFolder(string path)
    {
        var full = Path.GetFullPath(path);
        if (!Directory.Exists(full))
            return false;

        Folder = full;
        Rescan();
        return true;
    }

    public void Rescan()
    {
        if (!Directory.Exists(Folder))
        {
            Tracks = Array.Empty<string>();
            SelectedIndex = -1;
            return;
        }

        Tracks = Directory
            .EnumerateFiles(Folder)
            .Where(f => Extensions.Contains(Path.GetExtension(f)))
            .OrderBy(f => Path.GetFileName(f), StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        SelectedIndex = Tracks.Count == 0 ? -1 : Math.Clamp(SelectedIndex, 0, Tracks.Count - 1);
        if (SelectedIndex < 0 && Tracks.Count > 0)
            SelectedIndex = 0;
    }

    public void Select(int index)
    {
        if (index >= 0 && index < Tracks.Count)
            SelectedIndex = index;
    }

    public void SelectByPath(string path)
    {
        var full = Path.GetFullPath(path);
        var idx = Tracks
            .Select((t, i) => (t, i))
            .FirstOrDefault(x => string.Equals(x.t, full, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(idx.t))
            SelectedIndex = idx.i;
    }

    public void SelectByFileName(string fileName)
    {
        var idx = Tracks
            .Select((t, i) => (t, i))
            .FirstOrDefault(x => string.Equals(Path.GetFileName(x.t), fileName, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(idx.t))
            SelectedIndex = idx.i;
    }

    public bool SelectNext()
    {
        if (Tracks.Count == 0)
            return false;

        SelectedIndex = SelectedIndex < 0 ? 0 : (SelectedIndex + 1) % Tracks.Count;
        return true;
    }
}