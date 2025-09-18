using SensorIngestApi.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SensorIngestApi.Services
{
    public class ThroughputStats : IThroughputStats
    {
        private readonly ConcurrentQueue<long> _ticks = new();
        private readonly object _trimLock = new();
        private readonly int _windowSeconds;
        private long _totalProcessed;
        public int EstimatedQueueLength { get; set; }

        public ThroughputStats(int windowSeconds = 10) => _windowSeconds = windowSeconds;

        public long TotalProcessed => Interlocked.Read(ref _totalProcessed);

        public void MarkOne()
        {
            Interlocked.Increment(ref _totalProcessed);
            _ticks.Enqueue(Stopwatch.GetTimestamp());
            Trim();
        }

        public double GetPerSecond()
        {
            Trim();
            var count = _ticks.Count;
            return count / (double)_windowSeconds;
        }

        private void Trim()
        {
            var now = Stopwatch.GetTimestamp();
            var threshold = now - Stopwatch.Frequency * _windowSeconds;
            lock (_trimLock)
            {
                while (_ticks.TryPeek(out var t) && t < threshold)
                    _ticks.TryDequeue(out _);
            }
        }
    }
}
