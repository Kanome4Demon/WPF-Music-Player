using System.Collections.ObjectModel;
using System.ComponentModel;

namespace WPFMusicPlayerDemo.Model.Entities
{
    public class Playlist
    {
        private bool _isSelected;
        public string Name { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        // ⚠️ 改成 ObservableCollection
        public ObservableCollection<PlaylistItem> Tracks { get; } = new ObservableCollection<PlaylistItem>();

        public void AddTrack(PlaylistItem item)
        {
            if (!Tracks.Any(t => t.FilePath == item.FilePath))
                Tracks.Add(item);
        }

        public void RemoveTrack(string filePath)
        {
            var track = Tracks.FirstOrDefault(t => t.FilePath == filePath);
            if (track != null)
                Tracks.Remove(track);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class PlaylistItem
    {
        public int Index { get; set; }         // 歌单索引
        public string FilePath { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public TimeSpan Duration { get; set; }

        // 便于 DataGrid 显示
        public string DurationDisplay => Duration.TotalHours >= 1
       ? Duration.ToString(@"hh\:mm\:ss")
       : Duration.ToString(@"mm\:ss");
    }
}
