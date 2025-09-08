namespace WPFMusicPlayerDemo.Audio.Player
{
    public interface IAudioPlayer : IDisposable
    {
        bool IsPlaying { get; }
        TimeSpan CurrentTime { get; }
        TimeSpan TotalTime { get; }

        event Action<bool> OnPlayStateChanged;
        event Action<TimeSpan, TimeSpan> OnPositionChanged;
        event Action<Exception> PlaybackStopped;

        void Play(string filePath);
        void Pause();
        void Stop();
        void Seek(TimeSpan position);
    }
}
