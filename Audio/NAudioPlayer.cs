using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Windows.Media;
using System.Windows.Threading;

namespace WPFMusicPlayerDemo.Audio
{
    public class NAudioPlayer : IAudioPlayer
    {
        private IWavePlayer _waveOut;
        private AudioFileReader _audioFileReader;
        private ISampleProvider _sampleProvider;
        private bool _isPlayingInternal = false;
        private readonly object _lock = new();

        public bool IsPlaying { get; private set; }

        public TimeSpan CurrentTime => _audioFileReader?.CurrentTime ?? TimeSpan.Zero;
        public TimeSpan TotalTime => _audioFileReader?.TotalTime ?? TimeSpan.Zero;

        public event Action<bool> OnPlayStateChanged;
        public event Action<TimeSpan, TimeSpan> OnPositionChanged;
        public event Action<Exception> PlaybackStopped;

        public NAudioPlayer()
        {
            // 每帧刷新时钟，通知 UI
            CompositionTarget.Rendering += (s, e) =>
            {
                if (_audioFileReader != null && IsPlaying)
                    OnPositionChanged?.Invoke(CurrentTime, TotalTime);
            };
        }

        private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (!_isPlayingInternal)
                return;

            _isPlayingInternal = false;
            IsPlaying = false;

            // 通知 UI 播放状态改变
            OnPlayStateChanged?.Invoke(false);
            OnPositionChanged?.Invoke(TotalTime, TotalTime);

            // 🔹 触发 PlaybackStopped 事件
            PlaybackStopped?.Invoke(e.Exception);
        }

        public void Play(string filePath)
        {
            lock (_lock)
            {
                if (_waveOut != null)
                    _waveOut.PlaybackStopped -= WaveOut_PlaybackStopped;

                Stop();

                try
                {
                    _audioFileReader = new AudioFileReader(filePath) { Volume = 1.0f };
                    _sampleProvider = new EqualizerSampleProvider(_audioFileReader);

                    _waveOut = new WasapiOut(AudioClientShareMode.Shared, 200);
                    _waveOut.Init(_sampleProvider);
                    _waveOut.PlaybackStopped += WaveOut_PlaybackStopped;

                    _waveOut.Play();
                    IsPlaying = true;
                    _isPlayingInternal = true;

                    OnPlayStateChanged?.Invoke(IsPlaying);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"无法播放文件: {ex.Message}");
                }
            }
        }

        public void Pause()
        {
            if (_waveOut == null) return;

            if (IsPlaying)
                _waveOut.Pause();
            else
                _waveOut.Play();

            IsPlaying = !IsPlaying;
            OnPlayStateChanged?.Invoke(IsPlaying);
        }

        public void Stop()
        {
            try { _waveOut?.Stop(); } catch { }

            var wave = _waveOut;
            var reader = _audioFileReader;

            _waveOut = null;
            _audioFileReader = null;
            IsPlaying = false;
            _isPlayingInternal = false;

            OnPlayStateChanged?.Invoke(IsPlaying);
            OnPositionChanged?.Invoke(TimeSpan.Zero, TimeSpan.Zero);

            Task.Run(() =>
            {
                wave?.Dispose();
                reader?.Dispose();
            });
        }

        public void Seek(TimeSpan position)
        {
            if (_audioFileReader != null)
            {
                if (position < TimeSpan.Zero) position = TimeSpan.Zero;
                if (position > TotalTime) position = TotalTime;
                _audioFileReader.CurrentTime = position;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
