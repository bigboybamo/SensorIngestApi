using Microsoft.EntityFrameworkCore;
using SensorIngestApi.Models;

namespace SensorIngestApi.Data
{
    public class TelemetryDbContext : DbContext
    {
        public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : base(options) { }

        public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
        public DbSet<Alert> Alerts => Set<Alert>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<SensorReading>(e =>
            {
                e.ToTable("readings");
                e.HasKey(e => new { e.Id, e.TimestampUtc, e.DeviceId });
                e.Property(x => x.DeviceId).HasColumnName("device_id").HasMaxLength(128);
                e.Property(x => x.TimestampUtc).HasColumnName("ts").HasColumnType("timestamptz");
                e.Property(x => x.Value).HasColumnName("value");
                e.HasIndex(x => x.TimestampUtc).HasDatabaseName("ix_readings_ts");
                e.HasIndex(x => new { x.DeviceId, x.TimestampUtc }).HasDatabaseName("ix_readings_device_ts");
            });

            b.Entity<Alert>(e =>
            {
                e.ToTable("alerts");
                e.HasKey(x => x.Id);
                e.Property(x => x.DeviceId).HasMaxLength(128);
                e.HasIndex(x => new { x.DeviceId, x.Utc });
            });
        }
    }
}
