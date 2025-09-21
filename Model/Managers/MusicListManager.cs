using System.Collections.ObjectModel;
using System.Collections.Generic;
using WPFMusicPlayerDemo.Model.Interfaces;
using WPFMusicPlayerDemo.Model.Entities;
using WPFMusicPlayerDemo.Model.Factory;

namespace WPFMusicPlayerDemo.Model.Managers
{
    public class MusicListManager : IMusicListManager
    {
        private readonly TrackFactory _trackFactory;

        public ObservableCollection<MusicTrack> MusicList { get; } = new ObservableCollection<MusicTrack>();

        public MusicListManager(TrackFactory trackFactory)
        {
            _trackFactory = trackFactory;
        }

        public void AddMusicFiles(IEnumerable<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                var track = _trackFactory.Create(filePath);
                MusicList.Add(track);
            }
        }
    }
}
