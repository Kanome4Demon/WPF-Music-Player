using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WPFMusicPlayerDemo.Audio;
using WPFMusicPlayerDemo.Queue;
using WPFMusicPlayerDemo.PlayModes;

namespace WPFMusicPlayerDemo.Core
{
    public class MusicPlayerController : IDisposable
    {
        private readonly IAudioPlayer _audioPlayer;
        private readonly IQueueManager _queueManager;
        private IPlayModeStrategy _playModeStrategy;
        private bool _isManualStop = false;

        public PlayMode CurrentPlayMode { get; private set; } = PlayMode.Sequential;
        public bool IsPlaying => _audioPlayer.IsPlaying;

        public event Action<bool> OnPlayStateChanged;
        public event Action<TimeSpan, TimeSpan> OnPositionChanged;
        public event Action<string> OnTrackChanged;

        public MusicPlayerController()
        {
            _audioPlayer = new NAudioPlayer();          // 注入音频播放实现
            _queueManager = new DefaultQueueManager();  // 注入队列管理实现

            // 事件订阅
            _audioPlayer.OnPlayStateChanged += state => OnPlayStateChanged?.Invoke(state);
            _audioPlayer.OnPositionChanged += (cur, total) => OnPositionChanged?.Invoke(cur, total);

            // 🔹 自动播放下一首
            _audioPlayer.PlaybackStopped += ex =>
            {
                if (_isManualStop || ex != null) return;

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    string nextTrack = _playModeStrategy.GetNextTrack(_queueManager);
                    if (nextTrack != null)
                        Play(nextTrack);
                }));
            };

            // 默认模式
            SetPlayMode(CurrentPlayMode);
        }

        #region PlayMode
        public void SetPlayMode(PlayMode mode)
        {
            CurrentPlayMode = mode;
            _playModeStrategy = mode switch
            {
                PlayMode.Sequential => new SequentialMode(),
                PlayMode.Shuffle => new ShuffleMode(),
                PlayMode.RepeatOne => new RepeatOneMode(),
                PlayMode.StopAfterCurrent => new StopAfterCurrentMode(),
                _ => new SequentialMode()
            };
        }
        #endregion

        #region QueueManagement
        public void SetQueue(IEnumerable<string> files, int startIndex = 0)
        {
            _queueManager.SetQueue(files, startIndex);
            Play(_queueManager.GetCurrent());
        }

        public void AddToQueue(string filePath)
        {
            _queueManager.AddToQueue(filePath);
        }

        public void PlayAtIndex(int index)
        {
            if (index >= 0 && index < _queueManager.Count)
            {
                _queueManager.SetCurrentIndex(index);
                Play(_queueManager.GetCurrent());
            }
        }

        public void LoadPlaylist(Playlist playlist, int startIndex = 0)
        {
            if (playlist == null || playlist.Tracks.Count == 0) return;

            var paths = playlist.Tracks.Select(t => t.FilePath);
            _queueManager.SetQueue(paths, startIndex);
            Play(_queueManager.GetCurrent());
        }
        #endregion

        #region PlaybackControl
        public void Play(string filePath)
        {
            _isManualStop = false;  // 重置手动停止标记
            _audioPlayer.Play(filePath);
            OnTrackChanged?.Invoke(filePath);
        }

        public void TogglePlayPause()
        {
            _audioPlayer.Pause();
        }

        public void Stop()
        {
            _isManualStop = true;
            _audioPlayer.Stop();
        }

        public void Seek(TimeSpan position)
        {
            _audioPlayer.Seek(position);
        }

        public void NextTrack()
        {
            _isManualStop = true;
            string nextTrack = _playModeStrategy.GetNextTrack(_queueManager);
            if (nextTrack != null)
                Play(nextTrack);
            else if (CurrentPlayMode == PlayMode.StopAfterCurrent)
                Stop();
            _isManualStop = false;
        }

        public void PreviousTrack()
        {
            _isManualStop = true;
            string prevTrack = _playModeStrategy.GetPreviousTrack(_queueManager);
            if (prevTrack != null)
                Play(prevTrack);
            _isManualStop = false;
        }
        #endregion

        public void Dispose()
        {
            _audioPlayer.Dispose();
        }
    }
}
