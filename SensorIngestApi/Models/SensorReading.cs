namespace SensorIngestApi.Models
{
    public class SensorReading
    {
        public long Id { get; set; }
        public string DeviceId { get; set; } = null!;
        public DateTimeOffset TimestampUtc { get; set; }
        public double Value { get; set; }
    }
}
