using System;

namespace WPFMusicPlayerDemo.Model
{
    public class MusicTrack
    {
        public required string FileName { get; set; }
        public required string FilePath { get; set; }
        public TimeSpan Duration { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty; // 可显示 KB/MB
    }
}
