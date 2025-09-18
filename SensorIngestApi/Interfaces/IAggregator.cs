using SensorIngestApi.Models;

namespace SensorIngestApi.Interfaces
{
    public interface IAggregator
    {
        void Add(SensorReading reading);
        void TrackPerSecond(DateTime utc, double perSecond);
        GlobalStats GetGlobal();
        IReadOnlyList<DeviceStatsRow> GetTopDevices(int n);
        (double mean, double std, long n) Baseline(string deviceId);
    }
}
