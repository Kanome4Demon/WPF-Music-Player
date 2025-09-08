using System.Collections.Generic;

namespace WPFMusicPlayerDemo.Model.Entities
{
    public class Playlist
    {
        public string Name { get; set; }
        public List<PlaylistItem> Tracks { get; } = new List<PlaylistItem>();

        public void AddTrack(PlaylistItem item)
        {
            if (!Tracks.Exists(t => t.FilePath == item.FilePath))
                Tracks.Add(item);
        }

        public void RemoveTrack(string filePath)
        {
            Tracks.RemoveAll(t => t.FilePath == filePath);
        }
    }

}
