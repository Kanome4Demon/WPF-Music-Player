using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFMusicPlayerDemo.Core;
using WPFMusicPlayerDemo.Model;

namespace WPFMusicPlayerDemo
{
    public partial class MainWindow : Window
    {
        private MusicPlayerController player;
        private readonly MusicListManager _musicManager = new();
        private bool _isDragging = false;
        private ObservableCollection<QueueItem> _queueItems = new ObservableCollection<QueueItem>();
        private PlaylistManager _playlistManager = new PlaylistManager();
        private ObservableCollection<QueueItem> _playlistItems = new ObservableCollection<QueueItem>();

        public MainWindow()
        {
            InitializeComponent();
            player = new MusicPlayerController();
            DataContext = _musicManager;

            player.OnPlayStateChanged += UpdatePlayButton;
            player.OnTrackChanged += UpdateQueueSelection;

            InitializeProgress();
        }

        private void InitializeProgress()
        {
            player.OnPositionChanged += (current, total) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (!_isDragging)
                    {
                        ProgressSlider.Maximum = total.TotalSeconds;
                        ProgressSlider.Value = current.TotalSeconds;
                    }

                    CurrentTimeText.Text = current.ToString(@"mm\:ss");
                    TotalTimeText.Text = total.ToString(@"mm\:ss");
                });
            };
        }

        private void UpdateQueueSelection(string filePath)
        {
            var index = _queueItems.ToList().FindIndex(q => q.FilePath == filePath);
            if (index >= 0)
                QueueDataGrid.SelectedIndex = index; // 高亮当前播放曲目
        }

        private void ProgressSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e) => _isDragging = true;

        private void ProgressSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            player.Seek(TimeSpan.FromSeconds(ProgressSlider.Value));
        }

        private void AddMusic_Click(object sender, RoutedEventArgs e) => _musicManager.AddMusicFiles();

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MusicDataGrid.SelectedItem is MusicTrack track)
            {
                var tracks = MusicDataGrid.Items.Cast<MusicTrack>().ToList();
                int startIndex = MusicDataGrid.SelectedIndex;

                _queueItems.Clear();
                foreach (var t in tracks)
                {
                    _queueItems.Add(new QueueItem
                    {
                        Index = _queueItems.Count + 1,
                        Title = t.Title,
                        Artist = t.Artist,
                        Duration = t.Duration.ToString(@"mm\:ss"),
                        FilePath = t.FilePath
                    });
                }

                QueueDataGrid.ItemsSource = _queueItems;
                player.SetQueue(tracks.Select(t => t.FilePath), startIndex);
                QueueDataGrid.SelectedIndex = startIndex;
            }
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

            // ✅ 这里调用修改后的 LoadPlaylist
            player.LoadPlaylist(playlist, startIndex);
            QueueDataGrid.SelectedIndex = startIndex;
            player.PlayAtIndex(startIndex);
        }

        private void QueueDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (QueueDataGrid.SelectedItem is QueueItem item)
            {
                int index = _queueItems.IndexOf(item);
                player.PlayAtIndex(index);
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

                player.AddToQueue(track.FilePath);
                _queueItems.Add(new QueueItem
                {
                    Index = _queueItems.Count + 1,
                    Title = track.Title,
                    Artist = track.Artist,
                    Duration = track.Duration.ToString(@"mm\:ss"),
                    FilePath = track.FilePath
                });

                QueueDataGrid.ItemsSource = _queueItems;
            }
        }

        private void AddToPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (MusicDataGrid.SelectedItem is not MusicTrack track)
            {
                MessageBox.Show("请先选择一首音乐", "提示");
                return;
            }

            if (PlaylistComboBox.SelectedItem is not Playlist playlist)
            {
                MessageBox.Show("请先选择一个歌单", "提示");
                return;
            }

            if (playlist.Tracks.Any(t => t.FilePath == track.FilePath))
            {
                MessageBox.Show("该曲目已在当前歌单中", "提示");
                return;
            }

            playlist.AddTrack(new PlaylistItem
            {
                FilePath = track.FilePath,
                Title = track.Title,
                Artist = track.Artist,
                Duration = track.Duration
            });

            _playlistItems.Add(new QueueItem
            {
                Index = _playlistItems.Count + 1,
                Title = track.Title,
                Artist = track.Artist,
                Duration = track.Duration.ToString(@"mm\:ss"),
                FilePath = track.FilePath
            });
        }


        private void CreatePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            string playlistName = Microsoft.VisualBasic.Interaction.InputBox("请输入歌单名称：", "新建歌单", "我的歌单");
            if (string.IsNullOrWhiteSpace(playlistName)) return;

            var playlist = _playlistManager.CreatePlaylist(playlistName);
            PlaylistComboBox.Items.Add(playlist);
            PlaylistComboBox.SelectedItem = playlist;

            _playlistItems.Clear();
        }


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

        private void PlayModeButton_Click(object sender, RoutedEventArgs e)
        {
            switch (player.CurrentPlayMode)
            {
                case PlayMode.Sequential:
                    player.SetPlayMode(PlayMode.Shuffle);
                    PlayModeButton.Content = "随机播放";
                    break;

                case PlayMode.Shuffle:
                    player.SetPlayMode(PlayMode.RepeatOne);
                    PlayModeButton.Content = "单曲循环";
                    break;

                case PlayMode.RepeatOne:
                    player.SetPlayMode(PlayMode.StopAfterCurrent);
                    PlayModeButton.Content = "播完即停";
                    break;

                case PlayMode.StopAfterCurrent:
                    player.SetPlayMode(PlayMode.Sequential);
                    PlayModeButton.Content = "顺序循环";
                    break;
            }
        }



        private void PlayPauseButton_Click(object sender, RoutedEventArgs e) => player.TogglePlayPause();

        private void NextButton_Click(object sender, RoutedEventArgs e) => player.NextTrack();

        private void BackButton_Click(object sender, RoutedEventArgs e) => player.PreviousTrack();

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

        // 窗口控制按钮
        private void MaxRestoreButton_Click(object sender, RoutedEventArgs e) =>
            WindowHelper.ToggleMaxRestore(this, MainBorder, MaxRestoreButton);

        private void MinButton_Click(object sender, RoutedEventArgs e) => WindowHelper.Minimize(this);

        private void CloseButton_Click(object sender, RoutedEventArgs e) => WindowHelper.Close(this);

        private void Window_MouseDown(object sender, MouseButtonEventArgs e) => WindowHelper.Drag(this, e);

        private void GetUserInput() { string userInput = InputTextBox.Text; }
        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e) { }
    }
}
