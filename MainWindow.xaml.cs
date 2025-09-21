using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
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
using WPFMusicPlayerDemo.Views;
using YourNamespace.Services;

namespace WPFMusicPlayerDemo
{
    public partial class MainWindow : Window
    {
        private readonly MusicPlayerController _player;
        private readonly MusicListManager _musicManager;
        private readonly BackgroundColorManager _colorManager = new();
        private bool _isDragging = false;
        private bool isFocused = false;

        private ObservableCollection<QueueItem> _queueItems = new();

        public MainWindow()
        {
            InitializeComponent();

            // 初始化服务与管理器
            var audioService = new NAudioFileService();
            var metadataService = new TagLibMetadataService();
            _musicManager = new MusicListManager(new TrackFactory(audioService, metadataService));
            _player = new MusicPlayerController();

            DataContext = _musicManager;

            // === 初始化 CloudDriveView ===
            CloudDriveViewControl.SetPlayer(_player);
            CloudDriveViewControl.TrackDoubleClicked += TrackDoubleClicked_Handler;
            CloudDriveViewControl.AddToQueueRequested += AddToQueue_Handler;
            CloudDriveViewControl.AddToPlaylistRequested += AddToPlaylist_Handler;

            // === 初始化 PlaylistView ===
            PlaylistViewControl.SetPlayer(_player);
            PlaylistViewControl.TrackDoubleClicked += TrackDoubleClicked_Handler;
            PlaylistViewControl.AddToQueueRequested += AddToQueue_Handler;
            PlaylistViewControl.AddToPlaylistRequested += AddToPlaylist_Handler;

            // === 初始化 MenuListControl ===
            MenuListControl.PlaylistChanged += MenuListControl_PlaylistChanged;
            MenuListControl.CloudDriveClicked += MenuListControl_CloudDriveClicked;
            MenuListControl.PlaylistViewClicked += MenuListControl_PlaylistViewClicked;
            MenuListControl.PlaylistClicked += (s, playlist) =>
            {
                // 切换到歌单页面
                CloudDriveViewControl.Visibility = Visibility.Collapsed;
                PlaylistViewControl.Visibility = Visibility.Visible;

                // 更新 PlaylistViewControl 当前歌单
                PlaylistViewControl.CurrentPlaylist = playlist;
            };

            // 队列数据绑定
            MenuListControl.QueueGrid.ItemsSource = _queueItems;

            // === 背景颜色绑定 ===
            _colorManager.DominantColorChanged += color =>
            {
                Dispatcher.Invoke(() =>
                {
                    CloudDriveViewControl.UpdateBackgroundColor(color);
                    PlaylistViewControl.UpdateBackgroundColor(color);
                });
            };

            // 图片变化监听（更新背景主色）
            var dpd = System.ComponentModel.DependencyPropertyDescriptor
                .FromProperty(Image.SourceProperty, typeof(Image));
            dpd?.AddValueChanged(CloudDriveViewControl.MusicImage, (s, e) =>
            {
                if (CloudDriveViewControl.MusicImage.Source is BitmapSource bitmap)
                    _colorManager.UpdateImageSource(bitmap);
            });
            dpd?.AddValueChanged(PlaylistViewControl.MusicImage, (s, e) =>
            {
                if (PlaylistViewControl.MusicImage.Source is BitmapSource bitmap)
                    _colorManager.UpdateImageSource(bitmap);
            });

            // 播放器事件绑定
            _player.OnPlayStateChanged += UpdatePlayButton;
            _player.OnTrackChanged += UpdateQueueSelection;
            _player.OnTrackChanged += UpdateCurrentSongInfo;

            // Loaded 初始化
            Loaded += (s, e) =>
            {
                MainWindow_Loaded(s, e);

                // 预加载 PlaylistView，防止切换卡顿
                PlaylistViewControl.Visibility = Visibility.Visible;
                PlaylistViewControl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                PlaylistViewControl.Arrange(new Rect(0, 0, PlaylistViewControl.DesiredSize.Width, PlaylistViewControl.DesiredSize.Height));
                PlaylistViewControl.UpdateLayout();
                PlaylistViewControl.Visibility = Visibility.Collapsed;

                // 初始化背景颜色
                if (CloudDriveViewControl.MusicImage.Source is BitmapSource bitmap)
                    _colorManager.UpdateImageSource(bitmap);
            };

            InitializeProgress();
        }

        private void UpdateCurrentSongInfo(string filePath)
        {
            Dispatcher.Invoke(() =>
            {
                // 从播放器获取当前曲目信息
                var currentTrack = _player.CurrentTrack;
                if (currentTrack != null)
                {
                    SongTitleText.Text = string.IsNullOrEmpty(currentTrack.Title)
                        ? System.IO.Path.GetFileNameWithoutExtension(filePath)
                        : currentTrack.Title;

                    ArtistText.Text = string.IsNullOrEmpty(currentTrack.Artist)
                        ? "未知艺术家"
                        : currentTrack.Artist;
                }
                else
                {
                    SongTitleText.Text = "无播放歌曲";
                    ArtistText.Text = "";
                }
            });
        }


        #region 播放 / 队列 / 歌单

        private void TrackDoubleClicked_Handler(object? sender, MusicTrack track)
        {
            ObservableCollection<MusicTrack> sourceList;

            if (sender == CloudDriveViewControl)
            {
                sourceList = new ObservableCollection<MusicTrack>(
                    CloudDriveViewControl.MusicDataGrid.Items.Cast<MusicTrack>());
            }
            else if (sender == PlaylistViewControl && PlaylistViewControl.CurrentPlaylist != null)
            {
                sourceList = new ObservableCollection<MusicTrack>(
                    PlaylistViewControl.CurrentPlaylist.Tracks
                        .Select(p => new MusicTrack
                        {
                            FilePath = p.FilePath,
                            FileName = System.IO.Path.GetFileName(p.FilePath),
                            Title = p.Title,
                            Artist = p.Artist,
                            Duration = p.Duration
                        }));
            }
            else
            {
                sourceList = new ObservableCollection<MusicTrack>();
            }

            int startIndex = sourceList.IndexOf(track);
            if (startIndex < 0) startIndex = 0;

            // 更新播放队列显示
            _queueItems.Clear();
            int idx = 1;
            foreach (var t in sourceList)
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

            MenuListControl.QueueGrid.SelectedIndex = startIndex;

            _player.SetQueue(sourceList.Select(t => t.FilePath), startIndex);
            _player.PlayAtIndex(startIndex);
        }

        private void AddToQueue_Handler(object? sender, MusicTrack track)
        {
            if (_queueItems.Any(x => x.FilePath == track.FilePath)) return;

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

        private void AddToPlaylist_Handler(object? sender, MusicTrack track)
        {
            if (PlaylistViewControl.CurrentPlaylist == null)
            {
                MessageBox.Show("请先选择一个歌单");
                return;
            }

            var item = new PlaylistItem
            {
                Index = PlaylistViewControl.CurrentPlaylist.Tracks.Count + 1,
                FilePath = track.FilePath,
                Title = track.Title,
                Artist = track.Artist,
                Album = track.Album,
                Duration = track.Duration
            };
            PlaylistViewControl.CurrentPlaylist.AddTrack(item);
        }

        #endregion

        #region MenuListControl 事件回调

        private void MenuListControl_PlaylistChanged(object? sender, Playlist playlist)
        {
            PlaylistViewControl.CurrentPlaylist = playlist;
        }

        private void MenuListControl_CloudDriveClicked(object? sender, EventArgs e)
        {
            CloudDriveViewControl.Visibility = Visibility.Visible;
            PlaylistViewControl.Visibility = Visibility.Collapsed;

            if (CloudDriveViewControl.MusicImage.Source is BitmapSource bitmap)
                _colorManager.UpdateImageSource(bitmap);
        }

        private void MenuListControl_PlaylistViewClicked(object? sender, EventArgs e)
        {
            CloudDriveViewControl.Visibility = Visibility.Collapsed;
            PlaylistViewControl.Visibility = Visibility.Visible;

            if (PlaylistViewControl.MusicImage.Source is BitmapSource bitmap)
                _colorManager.UpdateImageSource(bitmap);
        }

        #endregion

        #region 播放器控制

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e) => _player.TogglePlayPause();
        private void NextButton_Click(object sender, RoutedEventArgs e) => _player.NextTrack();
        private void BackButton_Click(object sender, RoutedEventArgs e) => _player.PreviousTrack();

        private void PlayModeButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_player.CurrentPlayMode)
            {
                case PlayMode.Sequential: _player.SetPlayMode(PlayMode.Shuffle); break;
                case PlayMode.Shuffle: _player.SetPlayMode(PlayMode.RepeatOne); break;
                case PlayMode.RepeatOne: _player.SetPlayMode(PlayMode.StopAfterCurrent); break;
                case PlayMode.StopAfterCurrent: _player.SetPlayMode(PlayMode.Sequential); break;
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
            Dispatcher.Invoke(() =>
            {
                PlayPauseButton.ApplyTemplate();
                if (PlayPauseButton.Template.FindName("IconImage", PlayPauseButton) is Image iconImage)
                {
                    string imgPath = isPlaying
                        ? "pack://application:,,,/WPFMusicPlayerDemo;component/Assets/Images/Icons/pause.png"
                        : "pack://application:,,,/WPFMusicPlayerDemo;component/Assets/Images/Icons/play.png";

                    iconImage.Source = new BitmapImage(new Uri(imgPath, UriKind.Absolute));

                    // 🔹 根据不同图标调整 Margin
                    if (isPlaying)
                    {
                        // 暂停图标视觉中心偏中
                        iconImage.Margin = new Thickness(0, 0, 0, 0);
                    }
                    else
                    {
                        // 播放图标视觉中心偏左
                        iconImage.Margin = new Thickness(2.5, 1, 0, 0);
                    }
                }

                PlayPauseButton.ToolTip = isPlaying ? "暂停" : "播放";
            });
        }

        private void UpdateQueueSelection(string filePath)
        {
            var index = _queueItems.ToList().FindIndex(q => q.FilePath == filePath);
            if (index >= 0)
                MenuListControl.QueueGrid.SelectedIndex = index;
        }

        #endregion

        #region 窗口控制

        private void MaxRestoreButton_Click(object sender, RoutedEventArgs e) =>
            WindowHelper.ToggleMaxRestore(this, MainBorder, MaxRestoreButton);
        private void MinButton_Click(object sender, RoutedEventArgs e) => WindowHelper.Minimize(this);
        private void CloseButton_Click(object sender, RoutedEventArgs e) => WindowHelper.Close(this);
        private void Window_MouseDown(object sender, MouseButtonEventArgs e) => WindowHelper.Drag(this, e);

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

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int val = 2;
            DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_NCRENDERING_POLICY, ref val, sizeof(int));
            var cornerPref = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(uint));
            DwmExtendFrameIntoClientArea(hwnd, new MARGINS { cxLeftWidth = 1, cxRightWidth = 1, cyTopHeight = 1, cyBottomHeight = 1 });
        }

        #endregion

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        { }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool hasText = !string.IsNullOrEmpty(SearchTextBox.Text);
            PlaceholderTextBlock.Visibility = hasText ? Visibility.Collapsed : Visibility.Visible;
            ClearButton.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            SearchTextBox.Focus();
        }

        private void CustomButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 执行右侧按钮功能，比如打开过滤菜单或跳转
        }

        // 鼠标进入
        private void SearchContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            if (isFocused) return; // 如果已经选中，不要播放悬停动画
            var sb = (Storyboard)SearchContainer.Resources["HoverIn"];
            sb.Begin();
        }

        // 鼠标离开
        private void SearchContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isFocused) return; // 选中状态下，不回退
            var sb = (Storyboard)SearchContainer.Resources["HoverOut"];
            sb.Begin();
        }

        // 聚焦：点击输入框时触发
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            isFocused = true;
            var sb = (Storyboard)SearchContainer.Resources["FocusIn"];
            sb.Begin();
        }

        // 失焦：点击界面其他地方时触发
        private void SearchTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            isFocused = false;
            var sb = (Storyboard)SearchContainer.Resources["FocusOut"];
            sb.Begin();
        }


    }
}
