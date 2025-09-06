using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace WPFMusicPlayerDemo
{
    public class MusicPlayerController : IDisposable
    {
        private IWavePlayer _waveOut;
        private PlayQueueManager _queueManager = new PlayQueueManager();
        private AudioFileReader _audioFileReader;
        private ISampleProvider _sampleProvider;
        private bool _isManualStop = false;
        private readonly object _playLock = new object();
        private Random _random = new Random();
        private bool _isPlayingInternal = false;

        private List<int> _shuffleIndexes = new List<int>();
        private int _shufflePointer = 0;

        public PlayMode CurrentPlayMode { get; private set; } = PlayMode.Sequential;

        public bool IsPlaying { get; private set; }

        public event Action<bool> OnPlayStateChanged;
        public event Action<TimeSpan, TimeSpan> OnPositionChanged;
        public event Action<string> OnTrackChanged;

        public TimeSpan CurrentTime => _audioFileReader?.CurrentTime ?? TimeSpan.Zero;
        public TimeSpan TotalTime => _audioFileReader?.TotalTime ?? TimeSpan.Zero;

        public MusicPlayerController()
        {
            CompositionTarget.Rendering += (s, e) =>
            {
                if (_audioFileReader != null && IsPlaying)
                    OnPositionChanged?.Invoke(CurrentTime, TotalTime);
            };
        }

        #region PlayMode
        public void SetPlayMode(PlayMode mode)
        {
            CurrentPlayMode = mode;
            if (mode == PlayMode.Shuffle)
                PrepareShuffle();
        }

        private void PrepareShuffle()
        {
            _shuffleIndexes = Enumerable.Range(0, _queueManager.Count).ToList();
            for (int i = _shuffleIndexes.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (_shuffleIndexes[i], _shuffleIndexes[j]) = (_shuffleIndexes[j], _shuffleIndexes[i]);
            }
            _shufflePointer = 0;
        }

        private string GetNextShuffleTrack()
        {
            if (_shuffleIndexes.Count == 0) return null;

            int index = _shuffleIndexes[_shufflePointer];
            _shufflePointer = (_shufflePointer + 1) % _shuffleIndexes.Count;

            _queueManager.SetCurrentIndex(index);
            return _queueManager.GetCurrent();
        }
        #endregion

        public void SetQueue(IEnumerable<string> files, int startIndex = 0)
        {
            _queueManager.SetQueue(files, startIndex);
            if (CurrentPlayMode == PlayMode.Shuffle)
                PrepareShuffle();
            Play(_queueManager.GetCurrent());
        }

        public void Play(string filePath)
        {
            lock (_playLock)
            {
                if (_waveOut != null)
                    _waveOut.PlaybackStopped -= WaveOut_PlaybackStopped;

                StopInternal();

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
                    OnTrackChanged?.Invoke(filePath);
                    OnPositionChanged?.Invoke(CurrentTime, TotalTime);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"无法播放文件: {ex.Message}");
                }
            }
        }

        private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (!_isPlayingInternal || e.Exception != null) return;

            _isPlayingInternal = false;

            if (!_isManualStop)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    string nextTrack = GetNextTrackByPlayMode(autoPlay: true);
                    if (nextTrack != null)
                        Play(nextTrack);
                }));
            }
        }

        private string GetNextTrackByPlayMode(bool autoPlay)
        {
            switch (CurrentPlayMode)
            {
                case PlayMode.Sequential:
                    return _queueManager.Next();

                case PlayMode.Shuffle:
                    return GetNextShuffleTrack();

                case PlayMode.RepeatOne:
                    return _queueManager.GetCurrent();

                case PlayMode.StopAfterCurrent:
                    if (_queueManager.CurrentIndex < _queueManager.Count - 1)
                        return _queueManager.Next();
                    return null;
            }

            return null;
        }

        public void TogglePlayPause()
        {
            if (_waveOut == null) return;

            if (IsPlaying)
                _waveOut.Pause();
            else
                _waveOut.Play();

            IsPlaying = !IsPlaying;
            OnPlayStateChanged?.Invoke(IsPlaying);
        }

        private void StopInternal()
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

        public void Stop()
        {
            _isManualStop = true;
            StopInternal();
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

        public void NextTrack()
        {
            _isManualStop = true;

            string nextTrack = GetNextTrackByPlayMode(autoPlay: false);
            if (nextTrack != null)
                Play(nextTrack);
            else if (CurrentPlayMode == PlayMode.StopAfterCurrent)
                Stop();

            _isManualStop = false;
        }

        public void PreviousTrack()
        {
            _isManualStop = true;

            string prevTrack = null;

            switch (CurrentPlayMode)
            {
                case PlayMode.Sequential:
                case PlayMode.StopAfterCurrent:
                    prevTrack = _queueManager.Previous();
                    break;

                case PlayMode.Shuffle:
                    int index = _random.Next(_queueManager.Count);
                    _queueManager.SetCurrentIndex(index);
                    prevTrack = _queueManager.GetCurrent();
                    break;

                case PlayMode.RepeatOne:
                    prevTrack = _queueManager.GetCurrent();
                    break;
            }

            if (prevTrack != null)
                Play(prevTrack);

            _isManualStop = false;
        }

        public void AddToQueue(string filePath)
        {
            _queueManager.AddToQueue(filePath);
            if (CurrentPlayMode == PlayMode.Shuffle)
                PrepareShuffle();
        }

        public void PlayAtIndex(int index)
        {
            if (index >= 0 && index < _queueManager.Count)
            {
                _isManualStop = true;
                _queueManager.SetCurrentIndex(index);
                Play(_queueManager.GetCurrent());
                _isManualStop = false;
            }
        }

        /// <summary>
        /// ✅ 加载歌单：自动把 PlaylistItem 转成路径列表
        /// </summary>
        public void LoadPlaylist(Playlist playlist, int startIndex = 0)
        {
            if (playlist == null || playlist.Tracks.Count == 0) return;

            // 只取路径传给 PlayQueueManager
            var paths = playlist.Tracks.Select(t => t.FilePath);
            _queueManager.SetQueue(paths, startIndex);

            if (CurrentPlayMode == PlayMode.Shuffle)
                PrepareShuffle();

            Play(_queueManager.GetCurrent());
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
