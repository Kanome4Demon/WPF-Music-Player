using System;
using NAudio.Wave;

namespace WPFMusicPlayerDemo.Services
{
    public class NAudioFileService : IAudioFileService
    {
        public TimeSpan GetDuration(string filePath)
        {
            try
            {
                using var reader = new AudioFileReader(filePath);
                return reader.TotalTime;
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }
    }
}
