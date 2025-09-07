using WPFMusicPlayerDemo.Queue;

namespace WPFMusicPlayerDemo.PlayModes
{
    public class RepeatOneMode : IPlayModeStrategy
    {
        public string GetNextTrack(IQueueManager queue)
        {
            return queue.GetCurrent();
        }

        public string GetPreviousTrack(IQueueManager queue)
        {
            return queue.GetCurrent();
        }
    }
}
