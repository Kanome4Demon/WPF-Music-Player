public enum PlayMode
{
    Sequential,      // 顺序循环
    Shuffle,         // 随机循环
    RepeatOne,       // 单曲循环
    StopAfterCurrent // 播放完即停
}
public class PlayQueueManager
{
    private List<string> _queue = new List<string>();
    private Random _rand = new Random();

    public int CurrentIndex { get; private set; } = -1;
    public int Count => _queue.Count;

    public PlayMode PlayMode { get; set; } = PlayMode.Sequential; // 默认顺序循环

    public void SetQueue(IEnumerable<string> files, int startIndex = 0)
    {
        _queue = files.ToList();
        CurrentIndex = startIndex;
    }

    public string GetCurrent()
    {
        if (CurrentIndex >= 0 && CurrentIndex < _queue.Count)
            return _queue[CurrentIndex];
        return null;
    }

    public string Next()
    {
        if (_queue.Count == 0) return null;

        if (PlayMode == PlayMode.Shuffle)
        {
            int nextIndex;
            do
            {
                nextIndex = _rand.Next(_queue.Count);
            } while (_queue.Count > 1 && nextIndex == CurrentIndex); // 避免连续重复
            CurrentIndex = nextIndex;
        }
        else
        {
            CurrentIndex = (CurrentIndex + 1 < _queue.Count) ? CurrentIndex + 1 : 0;
        }

        return GetCurrent();
    }

    public string Previous()
    {
        if (_queue.Count == 0) return null;

        if (PlayMode == PlayMode.Shuffle)
        {
            int prevIndex;
            do
            {
                prevIndex = _rand.Next(_queue.Count);
            } while (_queue.Count > 1 && prevIndex == CurrentIndex);
            CurrentIndex = prevIndex;
        }
        else
        {
            CurrentIndex = (CurrentIndex - 1 >= 0) ? CurrentIndex - 1 : _queue.Count - 1;
        }

        return GetCurrent();
    }

    public void AddToQueue(string filePath)
    {
        if (!_queue.Contains(filePath))
            _queue.Add(filePath);
    }

    public void SetCurrentIndex(int index)
    {
        if (index >= 0 && index < _queue.Count)
            CurrentIndex = index;
    }

    public bool HasNext() => CurrentIndex + 1 < _queue.Count;
}
