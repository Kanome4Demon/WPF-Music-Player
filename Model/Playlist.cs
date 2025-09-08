
public class Playlist
{
    public string Name { get; set; }
    public List<PlaylistItem> Tracks { get; private set; } = new List<PlaylistItem>();

    public Playlist(string name) => Name = name;

    public void AddTrack(PlaylistItem item)
    {
        if (!Tracks.Any(t => t.FilePath == item.FilePath))
        {
            item.Index = Tracks.Count; // 设置索引
            Tracks.Add(item);
        }
    }
}