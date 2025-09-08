using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFMusicPlayerDemo.Core;
using WPFMusicPlayerDemo.Model;
using WPFMusicPlayerDemo.Model.Entities;
using WPFMusicPlayerDemo.Model.Interfaces;
using WPFMusicPlayerDemo.Model.Managers;
using WPFMusicPlayerDemo.Queue;
using WPFMusicPlayerDemo.Services;

namespace WPFMusicPlayerDemo
{
    public partial class MainWindow : Window
    {
        private readonly MusicPlayerController _player;
        private readonly MusicListManager _musicManager;
        private readonly IPlaylistManager _playlistManager;

        private bool _isDragging = false;
        private ObservableCollection<QueueItem> _queueItems = new ObservableCollection<QueueItem>();
        private ObservableCollection<QueueItem> _playlistItems = new ObservableCollection<QueueItem>();

        public MainWindow()
        {
            InitializeComponent();

            // 创建 Service
            var audioService = new NAudioFileService();
            var metadataService = new TagLibMetadataService();

            // 注入 MusicListManager
            _musicManager = new MusicListManager(new TrackFactory(audioService, metadataService));

            // 注入 PlaylistManager
            _playlistManager = new PlaylistManager();

            // 创建播放器控制器
            _player = new MusicPlayerController();

            DataContext = _musicManager;

            // 播放状态绑定
            _player.OnPlayStateChanged += UpdatePlayButton;
            _player.OnTrackChanged += UpdateQueueSelection;
            InitializeProgress();

            // 绑定歌单 ComboBox
            PlaylistComboBox.ItemsSource = _playlistManager.Playlists;
        }

        #region 文件添加
        private void AddMusic_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "音频文件|*.mp3;*.wav;*.flac;*.aac;*.wma"
            };

            if (dialog.ShowDialog() == true)
                _musicManager.AddMusicFiles(dialog.FileNames);
        }
        #endregion

        #region 播放队列
        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MusicDataGrid.SelectedItem is MusicTrack track)
            {
                var tracks = MusicDataGrid.Items.Cast<MusicTrack>().ToList();
                int startIndex = MusicDataGrid.SelectedIndex;

                _queueItems.Clear();
                int idx = 1;
                foreach (var t in tracks)
                {
                    _queueItems.Add(new QueueItem
                    {
                        Index = idx++,
                        Title = t.Title,
                        Artist = t.Artist,
                        Duration = t.Duration.ToString(@"mm\:ss"),
                        FilePath = t.FilePath
                    });
                }

                QueueDataGrid.ItemsSource = _queueItems;
                _player.SetQueue(tracks.Select(t => t.FilePath), startIndex);
                QueueDataGrid.SelectedIndex = startIndex;
            }
        }

        private void QueueDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (QueueDataGrid.SelectedItem is QueueItem item)
            {
                int index = _queueItems.IndexOf(item);
                _player.PlayAtIndex(index);
            }
        }

        private void AddToQueue_Click(object sender, RoutedEventArgs e)
        {
            if (MusicDataGrid.SelectedItem is MusicTrack track)
            {
                if (_queueItems.Any(x => x.FilePath == track.FilePath))
                {
                    MessageBox.Show("该曲目已在播放列表中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _player.AddToQueue(track.FilePath);
                _queueItems.Add(new QueueItem
                {
                    Index = _queueItems.Count + 1,
                    Title = track.Title,
                    Artist = track.Artist,
                    Duration = track.Duration.ToString(@"mm\:ss"),
                    FilePath = track.FilePath
                });
            }
        }
        #endregion

        #region 歌单管理
        private void PlaylistComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlaylistComboBox.SelectedItem is not Playlist playlist) return;

            _playlistItems.Clear();
            int idx = 1;
            foreach (var track in playlist.Tracks)
            {
                _playlistItems.Add(new QueueItem
                {
                    Index = idx++,
                    Title = track.Title,
                    Artist = track.Artist,
                    Duration = track.Duration.ToString(@"mm\:ss"),
                    FilePath = track.FilePath
                });
            }
            PlaylistDataGrid.ItemsSource = _playlistItems;
        }

        private void CreatePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            string playlistName = Microsoft.VisualBasic.Interaction.InputBox("请输入歌单名称：", "新建歌单", "我的歌单");
            if (string.IsNullOrWhiteSpace(playlistName)) return;

            var playlist = _playlistManager.CreatePlaylist(playlistName);
            PlaylistComboBox.SelectedItem = playlist;

            _playlistItems.Clear();
        }

        private void AddToPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (MusicDataGrid.SelectedItem is not MusicTrack track)
            {
                MessageBox.Show("请先选择音乐", "提示");
                return;
            }

            if (PlaylistComboBox.SelectedItem is not Playlist playlist)
            {
                MessageBox.Show("请先选择歌单", "提示");
                return;
            }

            _playlistManager.AddTrackToPlaylist(playlist, track);

            _playlistItems.Add(new QueueItem
            {
                Index = _playlistItems.Count + 1,
                Title = track.Title,
                Artist = track.Artist,
                Duration = track.Duration.ToString(@"mm\:ss"),
                FilePath = track.FilePath
            });
        }

        private void PlaylistDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PlaylistDataGrid.SelectedItem is not QueueItem item) return;
            if (PlaylistComboBox.SelectedItem is not Playlist playlist) return;

            int startIndex = playlist.Tracks.FindIndex(t => t.FilePath == item.FilePath);
            if (startIndex < 0) startIndex = 0;

            _queueItems.Clear();
            int idx = 1;
            foreach (var track in playlist.Tracks)
            {
                _queueItems.Add(new QueueItem
                {
                    Index = idx++,
                    Title = track.Title,
                    Artist = track.Artist,
                    Duration = track.Duration.ToString(@"mm\:ss"),
                    FilePath = track.FilePath
                });
            }

            QueueDataGrid.ItemsSource = _queueItems;
            _player.LoadPlaylist(playlist, startIndex);
            QueueDataGrid.SelectedIndex = startIndex;
            _player.PlayAtIndex(startIndex);
        }
        #endregion

        #region 播放控制
        private void PlayPauseButton_Click(object sender, RoutedEventArgs e) => _player.TogglePlayPause();
        private void NextButton_Click(object sender, RoutedEventArgs e) => _player.NextTrack();
        private void BackButton_Click(object sender, RoutedEventArgs e) => _player.PreviousTrack();

        private void PlayModeButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_player.CurrentPlayMode)
            {
                case PlayMode.Sequential:
                    _player.SetPlayMode(PlayMode.Shuffle);
                    PlayModeButton.Content = "随机播放";
                    break;
                case PlayMode.Shuffle:
                    _player.SetPlayMode(PlayMode.RepeatOne);
                    PlayModeButton.Content = "单曲循环";
                    break;
                case PlayMode.RepeatOne:
                    _player.SetPlayMode(PlayMode.StopAfterCurrent);
                    PlayModeButton.Content = "播完即停";
                    break;
                case PlayMode.StopAfterCurrent:
                    _player.SetPlayMode(PlayMode.Sequential);
                    PlayModeButton.Content = "顺序循环";
                    break;
            }
        }

        private void ProgressSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e) => _isDragging = true;

        private void ProgressSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _player.Seek(TimeSpan.FromSeconds(ProgressSlider.Value));
        }

        private void InitializeProgress()
        {
            _player.OnPositionChanged += (cur, total) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (!_isDragging)
                    {
                        ProgressSlider.Maximum = total.TotalSeconds;
                        ProgressSlider.Value = cur.TotalSeconds;
                    }

                    CurrentTimeText.Text = cur.ToString(@"mm\:ss");
                    TotalTimeText.Text = total.ToString(@"mm\:ss");
                });
            };
        }

        private void UpdatePlayButton(bool isPlaying)
        {
            PlayPauseButton.Content = new TextBlock
            {
                Text = isPlaying ? "\xE103" : "\xE768",
                FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
                FontSize = 20,
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private void UpdateQueueSelection(string filePath)
        {
            var index = _queueItems.ToList().FindIndex(q => q.FilePath == filePath);
            if (index >= 0)
                QueueDataGrid.SelectedIndex = index;
        }
        #endregion

        #region 窗口控制
        private void MaxRestoreButton_Click(object sender, RoutedEventArgs e) =>
            WindowHelper.ToggleMaxRestore(this, MainBorder, MaxRestoreButton);

        private void MinButton_Click(object sender, RoutedEventArgs e) => WindowHelper.Minimize(this);

        private void CloseButton_Click(object sender, RoutedEventArgs e) => WindowHelper.Close(this);

        private void Window_MouseDown(object sender, MouseButtonEventArgs e) => WindowHelper.Drag(this, e);
        #endregion

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e) { }
    }
}
