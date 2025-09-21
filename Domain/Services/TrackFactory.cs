using System;
using System.IO;
using WPFMusicPlayerDemo.Model.Entities;
using WPFMusicPlayerDemo.Services;

namespace WPFMusicPlayerDemo.Model.Factory
{
    public class TrackFactory
    {
        private readonly IAudioFileService _audioFileService;
        private readonly IMetadataService _metadataService;

        public TrackFactory(IAudioFileService audioFileService, IMetadataService metadataService)
        {
            _audioFileService = audioFileService;
            _metadataService = metadataService;
        }

        public MusicTrack Create(string filePath)
        {
            TimeSpan duration = _audioFileService.GetDuration(filePath);
            var fileInfo = new FileInfo(filePath);
            string format = fileInfo.Extension.TrimStart('.').ToUpper();
            string size = (fileInfo.Length / 1024.0 / 1024.0).ToString("F2") + " MB";

            var (title, artist, album) = _metadataService.GetMetadata(filePath);

            return new MusicTrack
            {
                FileName = fileInfo.Name,
                FilePath = filePath,
                Duration = duration,
                Title = title,
                Artist = artist,
                Album = album,
                Format = format,
                Size = size
            };
        }
    }
}
