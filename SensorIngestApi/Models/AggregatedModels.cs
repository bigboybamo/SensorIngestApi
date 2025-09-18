namespace SensorIngestApi.Models
{
    public class PerSecondPoint
    {
        public DateTime Utc { get; set; }
        public double PerSecond { get; set; }

        public PerSecondPoint() { }
        public PerSecondPoint(DateTime utc, double perSecond)
        {
            Utc = utc;
            PerSecond = perSecond;
        }
    }

    public class GlobalStats
    {
        public long Total { get; set; }
        public double AvgValue { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public IReadOnlyList<PerSecondPoint> PerSecond { get; set; } = Array.Empty<PerSecondPoint>();

        public GlobalStats() { }

        public GlobalStats(long total, double avgValue, double minValue, double maxValue, IReadOnlyList<PerSecondPoint> perSecond)
        {
            Total = total;
            AvgValue = avgValue;
            MinValue = minValue;
            MaxValue = maxValue;
            PerSecond = perSecond;
        }
    }

    public class DeviceStatsRow
    {
        public string DeviceId { get; set; } = default!;
        public long Count { get; set; }
        public double Avg { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double StdDev { get; set; }

        public DeviceStatsRow() { }

        public DeviceStatsRow(string deviceId, long count, double avg, double min, double max, double stdDev)
        {
            DeviceId = deviceId;
            Count = count;
            Avg = avg;
            Min = min;
            Max = max;
            StdDev = stdDev;
        }
    }

    public class StatsDto
    {
        public long TotalProcessed { get; set; }
        public double PerSecond { get; set; }
        public int QueueLength { get; set; }
        public GlobalStats Global { get; set; } = default!;
        public IReadOnlyList<DeviceStatsRow> TopDevices { get; set; } = Array.Empty<DeviceStatsRow>();

        public StatsDto() { }

        public StatsDto(long totalProcessed, double perSecond, int queueLength, GlobalStats global, IReadOnlyList<DeviceStatsRow> topDevices)
        {
            TotalProcessed = totalProcessed;
            PerSecond = perSecond;
            QueueLength = queueLength;
            Global = global;
            TopDevices = topDevices;
        }
    }

}
