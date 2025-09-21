using WPFMusicPlayerDemo.Queue;

namespace WPFMusicPlayerDemo.PlayModes
{
    public interface IPlayModeStrategy
    {
        /// <summary>
        /// 获取下一首歌的索引
        /// </summary>
        string GetNextTrack(IQueueManager queue);

        /// <summary>
        /// 获取上一首歌的索引
        /// </summary>
        string GetPreviousTrack(IQueueManager queue);
    }
}
