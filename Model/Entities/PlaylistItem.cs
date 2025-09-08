public class PlaylistItem
{
    public int Index { get; set; }         // 歌单索引
    public string FilePath { get; set; }
    public string Title { get; set; }
    public string Artist { get; set; }
    public TimeSpan Duration { get; set; }
}
