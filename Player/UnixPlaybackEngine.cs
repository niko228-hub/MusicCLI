using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MusicCli.Player;

internal sealed class UnixPlaybackEngine : IPlaybackEngine
{
    private Process? _process;
    private readonly object _gate = new();
    private volatile bool _finished = true;

    public string Name { get; private set; } = "unix";
    public bool IsFinished => _finished;

    public void Play(string filePath)
    {
        Stop();

        var backend = ResolveBackend()
            ?? throw new InvalidOperationException(
                "Системный проигрыватель не найден.\n" +
                "Linux: установите mpv, ffmpeg или mpg123.\n" +
                "  NixOS:  nix-shell -p mpv\n" +
                "  Debian: sudo apt install mpv\n" +
                "macOS: brew install mpv, либо воспользуйтесь встроенным afplay (wav/aiff).");

        var start = backend.Create(filePath);
        start.UseShellExecute = false;
        start.CreateNoWindow = true;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.WorkingDirectory = Path.GetDirectoryName(filePath) ?? Environment.CurrentDirectory;

        var process = new Process { StartInfo = start, EnableRaisingEvents = true };
        process.Exited += (_, _) =>
        {
            lock (_gate)
            {
                if (_process == process)
                {
                    _finished = true;
                    _process = null;
                }
            }
        };

        if (!process.Start())
            throw new InvalidOperationException($"Не удалось запустить {backend.Name}");

        _ = Drain(process.StandardOutput);
        _ = Drain(process.StandardError);

        lock (_gate)
        {
            _process = process;
            Name = backend.Name;
            _finished = false;
        }
    }

    public void Pause()
    {
        lock (_gate)
        {
            if (_process is null || _process.HasExited)
                return;
            SendSignal(_process.Id, stop: true);
        }
    }

    public void Resume()
    {
        lock (_gate)
        {
            if (_process is null || _process.HasExited)
                return;
            SendSignal(_process.Id, stop: false);
        }
    }

    public void Stop()
    {
        Process? process;
        lock (_gate)
        {
            process = _process;
            _process = null;
            _finished = true;
        }

        if (process is null)
            return;

        try
        {
            if (!process.HasExited)
            {
                try { SendSignal(process.Id, stop: false); } catch { }
                process.Kill(entireProcessTree: true);
                process.WaitForExit(1500);
            }
        }
        catch
        {
        }
        finally
        {
            process.Dispose();
        }
    }

    public void Dispose() => Stop();

    private static async Task Drain(StreamReader reader)
    {
        try { await reader.ReadToEndAsync(); }
        catch { }
    }

    private static Backend? ResolveBackend()
    {
        foreach (var backend in Backend.All)
        {
            if (FindOnPath(backend.Executable) is not null)
                return backend;
        }

        return null;
    }

    private static string? FindOnPath(string exe)
    {
        var paths = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var dir in paths)
        {
            var candidate = Path.Combine(dir, exe);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static void SendSignal(int pid, bool stop)
    {
        var code = stop
            ? (OperatingSystem.IsMacOS() ? 17 : 19)
            : (OperatingSystem.IsMacOS() ? 19 : 18);

        if (NativeKill(pid, code) != 0)
            throw new InvalidOperationException($"Не удалось отправить сигнал процессу {pid}");
    }

    [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static extern int NativeKill(int pid, int sig);

    private sealed record Backend(string Name, string Executable, Func<string, ProcessStartInfo> Create)
    {
        public static readonly Backend[] All =
        [
            new("mpv", "mpv", file => new ProcessStartInfo
            {
                FileName = "mpv",
                ArgumentList = { "--no-video", "--really-quiet", "--no-terminal", "--audio-display=no", file }
            }),
            new("ffplay", "ffplay", file => new ProcessStartInfo
            {
                FileName = "ffplay",
                ArgumentList = { "-nodisp", "-autoexit", "-loglevel", "quiet", file }
            }),
            new("mpg123", "mpg123", file => new ProcessStartInfo
            {
                FileName = "mpg123",
                ArgumentList = { "-q", "-o", "pulse", file }
            }),
            new("afplay", "afplay", file => new ProcessStartInfo
            {
                FileName = "afplay",
                ArgumentList = { file }
            })
        ];
    }
}