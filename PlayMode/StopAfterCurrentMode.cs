using WPFMusicPlayerDemo.Queue;

namespace WPFMusicPlayerDemo.PlayModes
{
    public class StopAfterCurrentMode : IPlayModeStrategy
    {
        public string GetNextTrack(IQueueManager queue)
        {
            if (queue.CurrentIndex < queue.Count - 1)
                return queue.Next();
            return null;
        }

        public string GetPreviousTrack(IQueueManager queue)
        {
            if (queue.CurrentIndex > 0)
                return queue.Previous();
            return null;
        }
    }
}
