namespace WPFMusicPlayerDemo.Queue
{
    public interface IQueueManager
    {
        int CurrentIndex { get; }
        int Count { get; }

        void SetQueue(IEnumerable<string> files, int startIndex = 0);

        string GetCurrent();
        string Next();
        string Previous();

        void AddToQueue(string filePath);
        void SetCurrentIndex(int index);

        bool HasNext();
    }
}
