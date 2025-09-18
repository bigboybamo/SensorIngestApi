namespace SensorIngestApi.Models
{
    public class Alert
    {
        public long Id { get; set; }
        public string DeviceId { get; set; } = null!;
        public DateTime Utc { get; set; }
        public double Value { get; set; }
        public string Kind { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}
