using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WPFMusicPlayerDemo.Core;
using WPFMusicPlayerDemo.Model.Entities;
using WPFMusicPlayerDemo.Model.Managers;
using WPFMusicPlayerDemo.Queue;

namespace WPFMusicPlayerDemo.Views
{
    public partial class PlaylistView : UserControl
    {
        private MusicPlayerController? _player;
        private Playlist? _currentPlaylist;
        private bool isUserScrolling = false;
        private double _velocity = 0;
        private bool _isAnimating;
        private DispatcherTimer _hideTimer;
        private bool _isMouseOverScrollBar = false;

        // 外部事件
        public event EventHandler<MusicTrack>? AddToPlaylistRequested;
        public event EventHandler<MusicTrack>? TrackDoubleClicked;
        public event EventHandler<MusicTrack>? AddToQueueRequested;
        public event Action<BitmapSource>? MusicImageChanged;

        public PlaylistView()
        {
            InitializeComponent();
            RegisterImageSourceChanged();
            Loaded += CloudDriveView_Loaded;
            MainScrollViewer.ScrollChanged += MainScrollViewer_ScrollChanged;
            _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
            _hideTimer.Tick += (s, e) =>
            {
                if (!_isMouseOverScrollBar)
                    FadeOutScrollBar();
                _hideTimer.Stop();
            };
        }

        // 外部注入播放器
        public void SetPlayer(MusicPlayerController player)
        {
            _player = player;
        }

        // 当前显示的歌单
        public Playlist? CurrentPlaylist
        {
            get => _currentPlaylist;
            set
            {
                _currentPlaylist = value;
                if (_currentPlaylist != null)
                    PlaylistDataGrid.ItemsSource = _currentPlaylist.Tracks;
            }
        }

        #region 滚动条逻辑

        private void CloudDriveView_Loaded(object sender, RoutedEventArgs e)
        {
            CustomScrollBar.Opacity = 0;

            // 绑定鼠标悬停事件：Thumb 和 Track 都触发
            void AttachHoverHandlers(UIElement element)
            {
                element.MouseEnter += (s, ev) =>
                {
                    _isMouseOverScrollBar = true;
                    FadeInScrollBar();
                    _hideTimer.Stop();
                };

                element.MouseLeave += (s, ev) =>
                {
                    _isMouseOverScrollBar = false;
                    StartHideTimer();
                };
            }

            AttachHoverHandlers(Thumb);
            AttachHoverHandlers(Track); // ✅ 新增：轨道也能触发淡入淡出

            // 拖动 Thumb 控制 ScrollViewer
            Thumb.DragDelta += (s, ev) =>
            {
                double trackHeight = Track.ActualHeight - Thumb.ActualHeight;
                if (trackHeight <= 0) return;

                double ratio = ev.VerticalChange / trackHeight;
                double newOffset = MainScrollViewer.VerticalOffset + ratio * MainScrollViewer.ScrollableHeight;
                newOffset = Math.Max(0, Math.Min(MainScrollViewer.ScrollableHeight, newOffset));
                MainScrollViewer.ScrollToVerticalOffset(newOffset);
            };

            _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.8) };
            _hideTimer.Tick += (s, ev) =>
            {
                _hideTimer.Stop();
                if (!_isMouseOverScrollBar)
                    FadeOutScrollBar();
            };

            UpdateCustomScrollBar();
        }

        private void FadeInScrollBar()
        {
            var fade = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            fade.Freeze();
            CustomScrollBar.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        private void FadeOutScrollBar()
        {
            var fade = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            fade.Freeze();
            CustomScrollBar.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        private void StartHideTimer()
        {
            _hideTimer.Stop();
            _hideTimer.Start();
        }

        private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            _velocity -= e.Delta / 15.0;

            FadeInScrollBar();
            StartHideTimer();

            if (!_isAnimating)
                CompositionTarget.Rendering += SmoothScrollStep;
        }

        private void SmoothScrollStep(object sender, EventArgs e)
        {
            if (Math.Abs(_velocity) < 0.2)
            {
                _velocity = 0;
                _isAnimating = false;
                CompositionTarget.Rendering -= SmoothScrollStep;
                return;
            }

            _isAnimating = true;

            double newOffset = MainScrollViewer.VerticalOffset + _velocity * 0.4;
            newOffset = Math.Max(0, Math.Min(MainScrollViewer.ScrollableHeight, newOffset));
            MainScrollViewer.ScrollToVerticalOffset(newOffset);

            _velocity *= 0.92;
        }

        private void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateCustomScrollBar();
        }

        private void UpdateCustomScrollBar()
        {
            if (MainScrollViewer.ExtentHeight <= MainScrollViewer.ViewportHeight)
            {
                CustomScrollBar.Visibility = Visibility.Collapsed;
                CustomScrollBar.Opacity = 0;
                _hideTimer.Stop();
                return;
            }

            CustomScrollBar.Visibility = Visibility.Visible;

            double trackHeight = Track.ActualHeight;

            // Thumb最小高度
            double minThumbHeight = 30;

            // 计算Thumb高度
            double thumbHeight = MainScrollViewer.ViewportHeight / MainScrollViewer.ExtentHeight * trackHeight;
            thumbHeight = Math.Max(minThumbHeight, thumbHeight);
            Thumb.Height = thumbHeight;

            // 可移动范围 = Track高度 - Thumb高度
            double maxThumbTop = trackHeight - thumbHeight;

            // ratio = 滚动比例
            double ratio = MainScrollViewer.ScrollableHeight == 0
                ? 0
                : MainScrollViewer.VerticalOffset / MainScrollViewer.ScrollableHeight;

            // 更新Thumb位置
            if (Thumb.RenderTransform is not TranslateTransform transform)
            {
                transform = new TranslateTransform();
                Thumb.RenderTransform = transform;
            }

            // 边界约束，确保不超过Track顶部/底部
            transform.Y = Math.Max(0, Math.Min(maxThumbTop, ratio * maxThumbTop));

            if (!_isMouseOverScrollBar)
                StartHideTimer();
        }

        #endregion

        #region 边框裁剪
        private void MusicBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Border border)
                border.Clip = new RectangleGeometry(new Rect(0, 0, border.ActualWidth, border.ActualHeight), 8, 8);
        }

        private void image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Border border && border.Child is Image img)
                img.Clip = new RectangleGeometry(new Rect(0, 0, border.ActualWidth, border.ActualHeight), 8, 8);
        }
        #endregion

        #region 添加音乐
        private void AddMusic_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "音频文件|*.mp3;*.wav;*.flac;*.aac;*.wma"
            };

            if (dialog.ShowDialog() == true && _currentPlaylist != null)
            {
                foreach (var file in dialog.FileNames)
                {
                    var track = new PlaylistItem
                    {
                        Index = _currentPlaylist.Tracks.Count + 1,
                        FilePath = file,
                        Title = System.IO.Path.GetFileNameWithoutExtension(file),
                        Artist = "未知",
                        Duration = TimeSpan.Zero // 如果有方法可以获取时长，这里可以赋值
                    };
                    _currentPlaylist.AddTrack(track);
                }
            }
        }
        #endregion

        #region 双击播放
        private void PlaylistDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PlaylistDataGrid.SelectedItem is PlaylistItem item)
            {
                var track = new MusicTrack
                {
                    FilePath = item.FilePath,
                    FileName = System.IO.Path.GetFileName(item.FilePath),
                    Title = item.Title,
                    Artist = item.Artist
                };
                TrackDoubleClicked?.Invoke(this, track);
            }
        }
        #endregion

        #region 加入队列
        private void AddToQueue_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistDataGrid.SelectedItem is PlaylistItem item)
            {
                var track = new MusicTrack
                {
                    FileName = System.IO.Path.GetFileName(item.FilePath),
                    FilePath = item.FilePath,
                    Title = item.Title,
                    Artist = item.Artist
                };
                AddToQueueRequested?.Invoke(this, track);
            }
        }
        #endregion

        #region 加入歌单
        private void AddToPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistDataGrid.SelectedItem is PlaylistItem item)
            {
                var track = new MusicTrack
                {
                    FileName = System.IO.Path.GetFileName(item.FilePath),
                    FilePath = item.FilePath,
                    Title = item.Title,
                    Artist = item.Artist
                };
                AddToPlaylistRequested?.Invoke(this, track);
            }
        }
        #endregion

        #region 图片变化事件
        private void RegisterImageSourceChanged()
        {
            var dpd = DependencyPropertyDescriptor.FromProperty(Image.SourceProperty, typeof(Image));
            if (dpd != null)
            {
                dpd.AddValueChanged(MusicImage, (s, e) =>
                {
                    if (MusicImage.Source is BitmapSource bitmap)
                        MusicImageChanged?.Invoke(bitmap);
                });
            }
        }

        public void UpdateBackgroundColor(Color color)
        {
            TopColor.Color = color;
        }
        #endregion

        #region DataGrid 表头交互
        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is DataGridColumnHeader header)
            {
                var column = header.Column;
                int state = column.SortDirection switch
                {
                    null => 0,
                    ListSortDirection.Ascending => 1,
                    ListSortDirection.Descending => 2,
                    _ => 0
                };
                state = (state + 1) % 3;

                foreach (var col in PlaylistDataGrid.Columns)
                    if (col != column) col.SortDirection = null;

                column.SortDirection = state switch
                {
                    0 => null,
                    1 => ListSortDirection.Ascending,
                    2 => ListSortDirection.Descending,
                    _ => null
                };

                var view = CollectionViewSource.GetDefaultView(PlaylistDataGrid.ItemsSource);
                view.SortDescriptions.Clear();

                if (state == 1)
                    view.SortDescriptions.Add(new SortDescription(column.SortMemberPath, ListSortDirection.Ascending));
                else if (state == 2)
                    view.SortDescriptions.Add(new SortDescription(column.SortMemberPath, ListSortDirection.Descending));

                view.Refresh();
            }
        }

        private void ColumnHeader_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimateAllHeaders(sender as DataGridColumnHeader, Color.FromRgb(136, 136, 136));
        }

        private void ColumnHeader_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimateAllHeaders(sender as DataGridColumnHeader, Colors.Transparent);
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
                    SolidColorBrush brush;
                    if (child.BorderBrush is SolidColorBrush solidBrush)
                        brush = solidBrush.IsFrozen ? new SolidColorBrush(solidBrush.Color) : solidBrush;
                    else
                        brush = new SolidColorBrush(Colors.Transparent);

                    child.BorderBrush = brush;
                    var anim = new ColorAnimation(targetColor, TimeSpan.FromMilliseconds(200));
                    brush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
                }
            }
        }
        #endregion
    }
}
