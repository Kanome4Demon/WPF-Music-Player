using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WPFMusicPlayerDemo.Model.Entities;
using WPFMusicPlayerDemo.Model.Interfaces;
using WPFMusicPlayerDemo.Model.Managers;
using WPFMusicPlayerDemo.Queue;

namespace WPFMusicPlayerDemo.Views
{
    public partial class MenuList : UserControl
    {
        private readonly IPlaylistManager _playlistManager;
        private bool _isRotated = false;
        private bool _popupOpen = false;
        private string current = "cloud";
        private Playlist? _selectedPlaylist;
        public DataGrid QueueGrid => QueueDataGrid;
        public ObservableCollection<Playlist> Playlists => _playlistManager.Playlists;

        public Playlist? SelectedPlaylist
        {
            get => _selectedPlaylist;
            set
            {
                _selectedPlaylist = value;

                // 同步更新 Playlists 中每个歌单的选中状态
                foreach (var p in Playlists)
                    p.IsSelected = (p == _selectedPlaylist);

                // 延迟刷新按钮显示，确保 ListBoxItem 已生成
                Dispatcher.BeginInvoke(new Action(UpdatePlaylistButtons),
                    System.Windows.Threading.DispatcherPriority.Loaded);

                // 更新显示文本
                UpdateSelectedPlaylistDisplay();

                // 通知外部逻辑
                PlaylistChanged?.Invoke(this, value);
            }
        }


        public ObservableCollection<QueueItem> QueueItems { get; } = new();

        public MenuList()
        {
            InitializeComponent();
            _playlistManager = new PlaylistManager();

            QueueDataGrid.ItemsSource = QueueItems;

            SetupPlaylistListBoxBehavior();

            this.MouseDown += Window_MouseDown;

            // 新增绑定
            PlaylistListBox.ItemsSource = Playlists;

            BtnCloud.Dispatcher.BeginInvoke(new Action(() =>
            {
                SelectionTranslate.X = GetButtonX(BtnCloud);
            }), System.Windows.Threading.DispatcherPriority.Loaded);

        }

        // --- 事件向外传递给 MainWindow ---
        public event EventHandler<Playlist>? PlaylistChanged;
        public event EventHandler? CloudDriveClicked;
        public event EventHandler? PlaylistViewClicked;
        public event EventHandler<Playlist>? PlaylistClicked;

        #region 滑块
        // 歌单按钮点击
        private void BtnPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (current == "playlist") return;
            current = "playlist";
            AnimateSelection(GetButtonX(BtnPlaylist));

            // 触发外部事件
            PlaylistViewClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnCloud_Click(object sender, RoutedEventArgs e)
        {
            if (current == "cloud") return;
            current = "cloud";
            AnimateSelection(GetButtonX(BtnCloud));

            // 触发外部事件
            CloudDriveClicked?.Invoke(this, EventArgs.Empty);
        }

        // 获取按钮在父 Grid 内的 X 坐标
        private double GetButtonX(Button btn)
        {
            return btn.TransformToAncestor(rootGrid)
                      .Transform(new Point(0, 0)).X;
        }

        // 滑动背景动画
        private void AnimateSelection(double toX)
        {
            var anim = new DoubleAnimation
            {
                To = toX,
                Duration = TimeSpan.FromSeconds(0.28),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            SelectionTranslate.BeginAnimation(TranslateTransform.XProperty, anim);
        }
        #endregion

        #region 创建按钮
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                if (_popupOpen)
                {
                    // 🔸 已打开 → 关闭
                    CloseCreatePlaylistPopup();
                }
                else
                {
                    // 🔸 未打开 → 打开
                    OpenCreatePlaylistPopup(btn);
                }
            }
        }

        private void OpenCreatePlaylistPopup(Button btn)
        {
            var rotate = btn.Template.FindName("PlusIconRotate", btn) as RotateTransform;
            if (rotate != null)
            {
                var anim = new DoubleAnimation(0, 45, TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                rotate.BeginAnimation(RotateTransform.AngleProperty, anim);
            }

            CreatePlaylistPopup.IsOpen = true;
            PlaylistNameTextBox.Focus();
            _isRotated = true;
            _popupOpen = true;
        }

        private void CloseCreatePlaylistPopup()
        {
            CreatePlaylistPopup.IsOpen = false;

            var rotate = CreateButton.Template.FindName("PlusIconRotate", CreateButton) as RotateTransform;
            if (rotate != null)
            {
                var anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                rotate.BeginAnimation(RotateTransform.AngleProperty, anim);
            }

            _isRotated = false;
            _popupOpen = false;
        }

        private void ApplyCreatePlaylist_Click(object sender, RoutedEventArgs e)
        {
            string name = PlaylistNameTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(name))
            {
                var playlist = _playlistManager.CreatePlaylist(name);
                SelectedPlaylist = playlist;  // 更新选中歌单
            }

            CloseCreatePlaylistPopup();
        }

        // 🔸 监听窗口任意点击，如果点击不在 Popup 或按钮上 → 关闭
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_popupOpen) return;

            var popup = CreatePlaylistPopup;
            var button = CreateButton;

            if (popup != null && button != null)
            {
                // 判断点击是否在 Popup 或 按钮内部
                var clickedElement = e.OriginalSource as DependencyObject;
                bool clickedInsidePopup = popup.IsAncestorOf(clickedElement);
                bool clickedOnButton = button.IsAncestorOf(clickedElement);

                if (!clickedInsidePopup && !clickedOnButton)
                {
                    CloseCreatePlaylistPopup();
                }
            }
        }

        #endregion

        #region listbox
        private void SetupPlaylistListBoxBehavior()
        {
            PlaylistListBox.Loaded += (s, e) =>
            {
                foreach (var item in PlaylistListBox.Items)
                {
                    if (PlaylistListBox.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem lbi)
                    {
                        SetupItemEvents(lbi);
                    }
                }

                // 动态生成的项
                PlaylistListBox.ItemContainerGenerator.StatusChanged += (s2, e2) =>
                {
                    if (PlaylistListBox.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                    {
                        foreach (var item in PlaylistListBox.Items)
                        {
                            if (PlaylistListBox.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem lbi)
                            {
                                SetupItemEvents(lbi);
                            }
                        }
                    }
                };
            };
        }

        private void SetupItemEvents(ListBoxItem lbi)
        {
            lbi.ApplyTemplate();

            lbi.Dispatcher.InvokeAsync(() =>
            {
                var border = FindChild<Border>(lbi, "RowBorder");
                if (border == null) return;

                // 悬停、选中、背景逻辑（保持你已有的代码）
                lbi.MouseEnter += (s, e) =>
                {
                    if (lbi.IsSelected) border.Background = new SolidColorBrush(Color.FromRgb(0x48, 0x48, 0x48));
                    else border.Background = new SolidColorBrush(Color.FromRgb(0x1f, 0x1f, 0x1f));
                };

                lbi.MouseLeave += (s, e) =>
                {
                    if (lbi.IsSelected) border.Background = new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x2a));
                    else border.Background = Brushes.Transparent;
                };

                // 使用 Bubbling 事件（MouseLeftButtonUp），而不是 Preview*
                lbi.MouseLeftButtonUp += (s, e) =>
                {
                    // 如果点击的是按钮或按钮的子元素，就跳过（保险检查）
                    if (e.OriginalSource is DependencyObject source && FindParent<Button>(source) != null)
                    {
                        // 不处理行点击
                        return;
                    }

                    // 正常行点击逻辑
                    if (lbi.DataContext is Playlist playlist)
                    {
                        PlaylistClicked?.Invoke(this, playlist);
                        AnimateSelection(GetButtonX(BtnPlaylist));
                        current = "playlist";
                    }

                    // 恢复背景（保留你原来的视觉逻辑）
                    if (lbi.IsMouseOver)
                    {
                        if (lbi.IsSelected) border.Background = new SolidColorBrush(Color.FromRgb(0x48, 0x48, 0x48));
                        else border.Background = new SolidColorBrush(Color.FromRgb(0x1f, 0x1f, 0x1f));
                    }
                    else
                    {
                        if (lbi.IsSelected) border.Background = new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x2a));
                        else border.Background = Brushes.Transparent;
                    }
                };

                // 选中/取消选中事件（视觉）
                lbi.Selected += (s, e) =>
                {
                    if (lbi.IsMouseOver) border.Background = new SolidColorBrush(Color.FromRgb(0x48, 0x48, 0x48));
                    else border.Background = new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x2a));
                };
                lbi.Unselected += (s, e) =>
                {
                    if (lbi.IsMouseOver) border.Background = new SolidColorBrush(Color.FromRgb(0x1f, 0x1f, 0x1f));
                    else border.Background = Brushes.Transparent;
                };
            });
        }


        private T? FindChild<T>(DependencyObject parent, string childName) where T : FrameworkElement
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T element && element.Name == childName)
                    return element;

                var result = FindChild<T>(child, childName);
                if (result != null)
                    return result;
            }
            return null;
        }

        private T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T found)
                    return found;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private void PlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Playlist playlist)
            {
                e.Handled = true;

                // 直接设置 SelectedPlaylist，后台会同步 IsSelected
                SelectedPlaylist = playlist;
            }
        }

        #endregion

        private void MusicBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Border border)
            {
                border.Clip = new RectangleGeometry(new Rect(0, 0, border.ActualWidth, border.ActualHeight), 8, 8);
            }
        }

        private void UpdateSelectedPlaylistDisplay()
        {
            if (PlaylistDisplay != null)
                PlaylistDisplay.Text = SelectedPlaylist?.Name ?? "未选择歌单";
        }

        private void UpdatePlaylistButtons()
        {
            foreach (var item in PlaylistListBox.Items)
            {
                if (PlaylistListBox.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem lbi)
                {
                    // 找到两个按钮
                    var uncheckBtn = FindChild<Button>(lbi, "UncheckBtn");
                    var checkBtn = FindChild<Button>(lbi, "CheckBtn");
                    if (item is Playlist playlist && uncheckBtn != null && checkBtn != null)
                    {
                        if (playlist.IsSelected)
                        {
                            checkBtn.Visibility = Visibility.Visible;
                            uncheckBtn.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            checkBtn.Visibility = Visibility.Collapsed;
                            uncheckBtn.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }


        //private void PlaylistComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (PlaylistComboBox.SelectedItem is Playlist playlist)
        //        PlaylistChanged?.Invoke(this, playlist);
        //}

        //private void CreatePlaylistButton_Click(object sender, RoutedEventArgs e)
        //{
        //    string name = Microsoft.VisualBasic.Interaction.InputBox("请输入歌单名称：", "新建歌单", "我的歌单");
        //    if (string.IsNullOrWhiteSpace(name)) return;

        //    var playlist = _playlistManager.CreatePlaylist(name);
        //    PlaylistComboBox.SelectedItem = playlist;
        //}

        //private void BtnCloudDrive_Click(object sender, RoutedEventArgs e) =>
        //    CloudDriveClicked?.Invoke(this, EventArgs.Empty);

        //private void BtnPlaylist_Click(object sender, RoutedEventArgs e) =>
        //    PlaylistViewClicked?.Invoke(this, EventArgs.Empty);
    }
}
