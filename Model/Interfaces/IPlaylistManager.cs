using System.Collections.ObjectModel;
using WPFMusicPlayerDemo.Model.Entities;

namespace WPFMusicPlayerDemo.Model.Interfaces
{
    public interface IPlaylistManager
    {
        ObservableCollection<Playlist> Playlists { get; }
        Playlist CreatePlaylist(string name);
        void AddTrackToPlaylist(Playlist playlist, MusicTrack track);
        void RemoveTrackFromPlaylist(Playlist playlist, MusicTrack track);
    }
}
