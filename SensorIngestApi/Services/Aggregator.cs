using SensorIngestApi.Interfaces;
using SensorIngestApi.Models;
using System.Collections.Concurrent;

namespace SensorIngestApi.Services
{
    public class Aggregator : IAggregator
    {
        private class DevStats
        {
            public long Count;
            public double Sum;
            public double Min = double.MaxValue;
            public double Max = double.MinValue;

            // Welford
            public long N;
            public double Mean;
            public double M2;

            public void Add(double v)
            {
                Count++; Sum += v;
                if (v < Min) Min = v;
                if (v > Max) Max = v;

                N++;
                var delta = v - Mean;
                Mean += delta / N;
                var delta2 = v - Mean;
                M2 += delta * delta2;
            }

            public double Avg => Count == 0 ? 0 : Sum / Count;
            public double Std => N > 1 ? Math.Sqrt(M2 / (N - 1)) : 0;
        }

        private readonly ConcurrentDictionary<string, DevStats> _dev = new();

        private readonly object _globalLock = new();
        private long _gCount;
        private double _gSum;
        private double _gMin = double.MaxValue;
        private double _gMax = double.MinValue;

        private readonly ConcurrentQueue<PerSecondPoint> _perSecond = new();
        private const int KeepSeconds = 120;

        public void Add(SensorReading r)
        {
            var ds = _dev.GetOrAdd(r.DeviceId, _ => new DevStats());
            ds.Add(r.Value);

            lock (_globalLock)
            {
                _gCount++;
                _gSum += r.Value;
                if (r.Value < _gMin) _gMin = r.Value;
                if (r.Value > _gMax) _gMax = r.Value;
            }
        }

        public void TrackPerSecond(DateTime utc, double perSecond)
        {
            _perSecond.Enqueue(new PerSecondPoint(utc, perSecond));
            while (_perSecond.Count > KeepSeconds) _perSecond.TryDequeue(out _);
        }

        public GlobalStats GetGlobal()
        {
            lock (_globalLock)
            {
                var avg = _gCount == 0 ? 0 : _gSum / _gCount;
                var min = _gMin == double.MaxValue ? 0 : _gMin;
                var max = _gMax == double.MinValue ? 0 : _gMax;
                return new GlobalStats(_gCount, avg, min, max, _perSecond.ToArray());
            }
        }

        public IReadOnlyList<DeviceStatsRow> GetTopDevices(int n)
        {
            var rows = _dev.Select(kv =>
            {
                var ds = kv.Value;
                return new DeviceStatsRow(kv.Key, ds.Count, ds.Avg, ds.Min, ds.Max, ds.Std);
            })
            .OrderByDescending(x => x.Count)
            .Take(n)
            .ToList();

            return rows;
        }

        public (double mean, double std, long n) Baseline(string deviceId)
        {
            if (_dev.TryGetValue(deviceId, out var ds))
                return (ds.Mean, ds.Std, ds.N);
            return (0, 0, 0);
        }
    }
}
