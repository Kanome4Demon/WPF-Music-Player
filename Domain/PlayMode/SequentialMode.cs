using WPFMusicPlayerDemo.Queue;

namespace WPFMusicPlayerDemo.PlayModes
{
    public class SequentialMode : IPlayModeStrategy
    {
        public string GetNextTrack(IQueueManager queue)
        {
            return queue.Next();
        }

        public string GetPreviousTrack(IQueueManager queue)
        {
            return queue.Previous();
        }
    }
}
