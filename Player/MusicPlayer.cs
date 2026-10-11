using System;
using System.IO;
using System.Threading.Tasks;

namespace MusicCli.Player
{
    public enum PlaybackState
    {
        Stopped,
        Playing,
        Paused
    }

    public sealed class MusicPlayer : IDisposable
    {
        private readonly IPlaybackEngine _engine;
        private string? _currentFile;

        public PlaybackState State { get; private set; } = PlaybackState.Stopped;
        public bool Continuous { get; private set; }
        public string? CurrentFile => _currentFile;
        public string? CurrentName => _currentFile is null ? null : Path.GetFileName(_currentFile);
        public string Backend => _engine.Name;

        public MusicPlayer()
        {
            _engine = OperatingSystem.IsWindows()
                ? new WindowsPlaybackEngine()
                : new UnixPlaybackEngine();
        }

        public Task PlayAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Указанный файл не найден.", filePath);

            if (State == PlaybackState.Paused &&
                string.Equals(_currentFile, filePath, StringComparison.OrdinalIgnoreCase))
            {
                return ResumeAsync();
            }

            _engine.Stop();
            _engine.Play(filePath);
            _currentFile = filePath;
            State = PlaybackState.Playing;
            Continuous = true;
            return Task.CompletedTask;
        }

        public Task PauseAsync()
        {
            if (State != PlaybackState.Playing)
                return Task.CompletedTask;

            _engine.Pause();
            State = PlaybackState.Paused;
            Continuous = false;
            return Task.CompletedTask;
        }

        public Task ResumeAsync()
        {
            if (State != PlaybackState.Paused)
                return Task.CompletedTask;

            _engine.Resume();
            State = PlaybackState.Playing;
            Continuous = true;
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _engine.Stop();
            State = PlaybackState.Stopped;
            Continuous = false;
            return Task.CompletedTask;
        }

        public void RefreshState()
        {
            if (State == PlaybackState.Playing && _engine.IsFinished)
                State = PlaybackState.Stopped;
        }

        public void Dispose()
        {
            try { _engine.Dispose(); }
            catch { }
            State = PlaybackState.Stopped;
        }
    }
}