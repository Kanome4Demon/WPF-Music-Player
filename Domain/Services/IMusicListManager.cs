using System.Collections.ObjectModel;
using WPFMusicPlayerDemo.Model.Entities;

namespace WPFMusicPlayerDemo.Model.Interfaces
{
    public interface IMusicListManager
    {
        ObservableCollection<MusicTrack> MusicList { get; }
        void AddMusicFiles(IEnumerable<string> filePaths);
    }
}
