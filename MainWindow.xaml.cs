using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WPFMusicPlayerDemo.Comon;
using WPFMusicPlayerDemo.Core;
using WPFMusicPlayerDemo.Model.Entities;
using WPFMusicPlayerDemo.Model.Factory;
using WPFMusicPlayerDemo.Model.Interfaces;
using WPFMusicPlayerDemo.Model.Managers;
using WPFMusicPlayerDemo.Queue;
using WPFMusicPlayerDemo.Services;
using YourNamespace.Services;

namespace WPFMusicPlayerDemo
{
    public partial class MainWindow : Window
    {
        private readonly MusicPlayerController _player;
        private readonly MusicListManager _musicManager;
        private readonly IPlaylistManager _playlistManager;
        private readonly BackgroundColorManager _colorManager = new();

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
            Loaded += MainWindow_Loaded;
            InitializeProgress();

            // 绑定歌单 ComboBox
            PlaylistComboBox.ItemsSource = _playlistManager.Playlists;

            _colorManager.DominantColorChanged += color =>
            {
                // 确保在 UI 线程更新
                Dispatcher.Invoke(() => TopColor.Color = color);
            };

            // 监听 MusicImage.Source 变化
            var dpd = System.ComponentModel.DependencyPropertyDescriptor
                .FromProperty(System.Windows.Controls.Image.SourceProperty, typeof(System.Windows.Controls.Image));
            dpd?.AddValueChanged(MusicImage, (s, e) =>
            {
                if (MusicImage.Source is BitmapSource bitmap)
                    _colorManager.UpdateImageSource(bitmap);
            });

            Loaded += (s, e) =>
            {
                if (MusicImage.Source is BitmapSource bitmap)
                    _colorManager.UpdateImageSource(bitmap);
            };

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            // 开启 DWM 系统阴影
            int val = 2; // DWMWA_NCRENDERING_POLICY
            DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_NCRENDERING_POLICY, ref val, sizeof(int));

            // 设置圆角 (Windows 11 有效)
            var cornerPref = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
                ref cornerPref, sizeof(uint));

            // 确保阴影可见
            DwmExtendFrameIntoClientArea(hwnd, new MARGINS { cxLeftWidth = 1, cxRightWidth = 1, cyTopHeight = 1, cyBottomHeight = 1 });
        }

        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is DataGridColumnHeader header)
            {
                var column = header.Column;

                // 获取当前排序状态：0=默认, 1=升序, 2=降序
                int state = column.SortDirection switch
                {
                    null => 0,
                    ListSortDirection.Ascending => 1,
                    ListSortDirection.Descending => 2,
                    _ => 0
                };

                // 三态循环
                state = (state + 1) % 3;

                // 清空其他列箭头
                foreach (var col in MusicDataGrid.Columns)
                {
                    if (col != column)
                        col.SortDirection = null;
                }

                // 设置当前列箭头显示
                column.SortDirection = state switch
                {
                    0 => null,
                    1 => ListSortDirection.Ascending,
                    2 => ListSortDirection.Descending,
                    _ => null
                };

                // 获取默认 CollectionView
                var view = CollectionViewSource.GetDefaultView(MusicDataGrid.ItemsSource);
                view.SortDescriptions.Clear();

                // 根据状态添加排序
                if (state == 1)
                    view.SortDescriptions.Add(new SortDescription(column.SortMemberPath, ListSortDirection.Ascending));
                else if (state == 2)
                    view.SortDescriptions.Add(new SortDescription(column.SortMemberPath, ListSortDirection.Descending));

                view.Refresh();
            }
        }

        private void MusicBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var border = sender as Border;
            border.Clip = new RectangleGeometry()
            {
                Rect = new Rect(0, 0, border.ActualWidth, border.ActualHeight),
                RadiusX = 8,
                RadiusY = 8
            };
        }

        private void image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var border = sender as Border;
            if (border.Child is Image img)
            {
                img.Clip = new RectangleGeometry()
                {
                    Rect = new Rect(0, 0, border.ActualWidth, border.ActualHeight),
                    RadiusX = 8,
                    RadiusY = 8
                };
            }
        }



        #region 表头悬停效果
        private void ColumnHeader_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimateAllHeaders(sender as DataGridColumnHeader, Color.FromRgb(136, 136, 136)); // 悬停颜色
        }

        private void ColumnHeader_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimateAllHeaders(sender as DataGridColumnHeader, Colors.Transparent); // 恢复透明
        }

        private void AnimateAllHeaders(DataGridColumnHeader header, Color targetColor)
        {
            if (header == null) return;

            var presenter = VisualTreeHelper.GetParent(header);
            if (presenter == null) return;

            int count = VisualTreeHelper.GetChildrenCount(presenter);
            for (int i = 0; i < count; i++)
            {
                if (VisualTreeHelper.GetChild(presenter, i) is DataGridColumnHeader child)
                {
                    // 如果原来的 BorderBrush 是冻结的，创建一个新的可动画的 Brush
                    SolidColorBrush brush = null;

                    if (child.BorderBrush is SolidColorBrush solidBrush)
                    {
                        brush = solidBrush.IsFrozen ? new SolidColorBrush(solidBrush.Color) : solidBrush;
                    }
                    else
                    {
                        brush = new SolidColorBrush(Colors.Transparent);
                    }

                    child.BorderBrush = brush;

                    // 动画
                    var anim = new ColorAnimation(targetColor, TimeSpan.FromMilliseconds(200));
                    brush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
                }
            }
        }
        #endregion

        #region DWM Interop

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, ref int pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, MARGINS pMarInset);

        private enum DWMWINDOWATTRIBUTE
        {
            DWMWA_NCRENDERING_POLICY = 2,
            DWMWA_WINDOW_CORNER_PREFERENCE = 33,
        }

        private enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        #endregion DWM Interop

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

        #endregion 文件添加

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
                        Duration = t.Duration.ToAutoString(),
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

        #endregion 播放队列

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

        #endregion 歌单管理

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

        #endregion 播放控制

        #region 窗口控制

        private void MaxRestoreButton_Click(object sender, RoutedEventArgs e) =>
            WindowHelper.ToggleMaxRestore(this, MainBorder, MaxRestoreButton);

        private void MinButton_Click(object sender, RoutedEventArgs e) => WindowHelper.Minimize(this);

        private void CloseButton_Click(object sender, RoutedEventArgs e) => WindowHelper.Close(this);

        private void Window_MouseDown(object sender, MouseButtonEventArgs e) => WindowHelper.Drag(this, e);

        #endregion 窗口控制

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        { }
    }
}