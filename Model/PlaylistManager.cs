public class PlaylistManager
{
    private List<Playlist> _playlists = new List<Playlist>();
    public IEnumerable<Playlist> Playlists => _playlists;

    public Playlist CreatePlaylist(string name)
    {
        var playlist = new Playlist(name);
        _playlists.Add(playlist);
        return playlist;
    }

    public void DeletePlaylist(string name)
    {
        var playlist = _playlists.FirstOrDefault(p => p.Name == name);
        if (playlist != null)
            _playlists.Remove(playlist);
    }

    public Playlist GetPlaylist(string name) =>
        _playlists.FirstOrDefault(p => p.Name == name);
}