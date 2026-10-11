using System.Runtime.InteropServices;
using System.Text;
using NAudio.Wave;

namespace MusicCli.Player;

internal sealed class WindowsPlaybackEngine : IPlaybackEngine
{
    private WaveOutEvent? _output;
    private MediaFoundationReader? _reader;
    private string? _mciAlias;
    private bool _usingMci;
    private volatile bool _finished;

    public string Name => _usingMci ? "windows-mci" : "windows-naudio";
    public bool IsFinished => _finished;

    public void Play(string filePath)
    {
        Stop();
        _finished = false;

        if (TryPlayNAudio(filePath))
        {
            _usingMci = false;
            return;
        }

        PlayMci(filePath);
        _usingMci = true;
    }

    public void Pause()
    {
        if (_usingMci)
        {
            Mci.Send($"pause {Alias()}");
            return;
        }

        _output?.Pause();
    }

    public void Resume()
    {
        if (_usingMci)
        {
            Mci.Send($"resume {Alias()}");
            return;
        }

        _output?.Play();
    }

    public void Stop()
    {
        if (_usingMci && _mciAlias is not null)
        {
            try { Mci.Send($"stop {_mciAlias}"); } catch { }
            try { Mci.Send($"close {_mciAlias}"); } catch { }
            _mciAlias = null;
        }

        DisposeNAudio();
        _finished = true;
    }

    public void Dispose() => Stop();

    private bool TryPlayNAudio(string filePath)
    {
        try
        {
            _reader = new MediaFoundationReader(filePath);
            _output = new WaveOutEvent();
            _output.PlaybackStopped += (_, e) =>
            {
                _finished = true;
            };
            _output.Init(_reader);
            _output.Play();
            return true;
        }
        catch
        {
            DisposeNAudio();
            return false;
        }
    }

    private void PlayMci(string filePath)
    {
        _mciAlias = "musiccli";
        Mci.Send($"close {_mciAlias}", throwOnError: false);

        var escaped = filePath.Replace("\"", "");
        var open = $"open \"{escaped}\" type mpegvideo alias {_mciAlias}";
        try
        {
            Mci.Send(open);
        }
        catch
        {
            Mci.Send($"open \"{escaped}\" alias {_mciAlias}");
        }

        Mci.Send($"play {_mciAlias}");
    }

    private string Alias() => _mciAlias ?? "musiccli";

    private void DisposeNAudio()
    {
        try { _output?.Stop(); } catch { }
        _output?.Dispose();
        _output = null;
        _reader?.Dispose();
        _reader = null;
    }

    private static class Mci
    {
        public static void Send(string command, bool throwOnError = true)
        {
            var err = Native.mciSendString(command, null, 0, IntPtr.Zero);
            if (err == 0 || !throwOnError)
                return;

            var buffer = new StringBuilder(256);
            Native.mciGetErrorString(err, buffer, buffer.Capacity);
            throw new InvalidOperationException($"Windows MCI: {buffer}");
        }

        private static class Native
        {
            [DllImport("winmm.dll", EntryPoint = "mciSendStringW", CharSet = CharSet.Unicode)]
            public static extern int mciSendString(string command, StringBuilder? returnValue, int returnLength, IntPtr winHandle);

            [DllImport("winmm.dll", EntryPoint = "mciGetErrorStringW", CharSet = CharSet.Unicode)]
            public static extern int mciGetErrorString(int errorCode, StringBuilder buffer, int length);
        }
    }
}