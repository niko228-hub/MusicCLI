namespace MusicCli.Player;

internal interface IPlaybackEngine : IDisposable
{
    string Name { get; }
    bool IsFinished { get; }

    void Play(string filePath);
    void Pause();
    void Resume();
    void Stop();
}