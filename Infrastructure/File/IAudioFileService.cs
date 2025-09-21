using System;

namespace WPFMusicPlayerDemo.Services
{
    public interface IAudioFileService
    {
        TimeSpan GetDuration(string filePath);
    }
}
