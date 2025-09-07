using System;
using System.Collections.Generic;
using System.Linq;
using WPFMusicPlayerDemo.Queue;

namespace WPFMusicPlayerDemo.PlayModes
{
    public class ShuffleMode : IPlayModeStrategy
    {
        private List<int> _shuffleIndexes = new List<int>();
        private int _shufflePointer = 0;
        private Random _random = new Random();

        private void PrepareShuffle(IQueueManager queue)
        {
            _shuffleIndexes = Enumerable.Range(0, queue.Count).ToList();
            for (int i = _shuffleIndexes.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (_shuffleIndexes[i], _shuffleIndexes[j]) = (_shuffleIndexes[j], _shuffleIndexes[i]);
            }
            _shufflePointer = 0;
        }

        public string GetNextTrack(IQueueManager queue)
        {
            if (_shuffleIndexes.Count == 0 || _shuffleIndexes.Count != queue.Count)
                PrepareShuffle(queue);

            int index = _shuffleIndexes[_shufflePointer];
            _shufflePointer = (_shufflePointer + 1) % _shuffleIndexes.Count;

            queue.SetCurrentIndex(index);
            return queue.GetCurrent();
        }

        public string GetPreviousTrack(IQueueManager queue)
        {
            if (_shuffleIndexes.Count == 0 || _shuffleIndexes.Count != queue.Count)
                PrepareShuffle(queue);

            _shufflePointer = (_shufflePointer - 1 + _shuffleIndexes.Count) % _shuffleIndexes.Count;
            int index = _shuffleIndexes[_shufflePointer];

            queue.SetCurrentIndex(index);
            return queue.GetCurrent();
        }
    }
}
