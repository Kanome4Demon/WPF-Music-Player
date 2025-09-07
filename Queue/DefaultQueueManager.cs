namespace WPFMusicPlayerDemo.Queue
{
    public class DefaultQueueManager : IQueueManager
    {
        private List<string> _queue = new();
        public int CurrentIndex { get; private set; } = -1;
        public int Count => _queue.Count;

        public void SetQueue(IEnumerable<string> files, int startIndex = 0)
        {
            _queue = files.ToList();
            CurrentIndex = (startIndex >= 0 && startIndex < _queue.Count) ? startIndex : 0;
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

            CurrentIndex = (CurrentIndex + 1 < _queue.Count) ? CurrentIndex + 1 : 0;
            return GetCurrent();
        }

        public string Previous()
        {
            if (_queue.Count == 0) return null;

            CurrentIndex = (CurrentIndex - 1 >= 0) ? CurrentIndex - 1 : _queue.Count - 1;
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
}
