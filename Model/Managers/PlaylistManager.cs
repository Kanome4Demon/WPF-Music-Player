using System.Collections.ObjectModel;
using WPFMusicPlayerDemo.Model.Entities;
using WPFMusicPlayerDemo.Model.Interfaces;

namespace WPFMusicPlayerDemo.Model.Managers
{
    public class PlaylistManager : IPlaylistManager
    {
        public ObservableCollection<Playlist> Playlists { get; } = new ObservableCollection<Playlist>();

        public Playlist CreatePlaylist(string name)
        {
            var playlist = new Playlist { Name = name };
            Playlists.Add(playlist);
            return playlist;
        }

        public void AddTrackToPlaylist(Playlist playlist, MusicTrack track)
        {
            if (playlist == null || track == null) return;

            var item = new PlaylistItem
            {
                FilePath = track.FilePath,
                Title = track.Title,
                Artist = track.Artist,
                Duration = track.Duration
            };

            playlist.AddTrack(item);
        }

        public void RemoveTrackFromPlaylist(Playlist playlist, MusicTrack track)
        {
            if (playlist == null || track == null) return;

            playlist.RemoveTrack(track.FilePath);
        }
    }
}
