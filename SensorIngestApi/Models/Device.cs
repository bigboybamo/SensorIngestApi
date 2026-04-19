namespace SensorIngestApi.Models
{
    public class Device
    {
        public string DeviceId { get; set; } = null!; // EF Core sets this; null! because nullable is enabled
        public DateTimeOffset RegisteredAtUtc { get; set; }
    }
}