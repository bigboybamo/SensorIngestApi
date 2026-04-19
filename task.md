# Task: Add Device Registration Endpoint

## Goal

Add a `POST /devices` endpoint that registers a device by its `DeviceId`. Devices are persisted to the database. Registering the same device twice returns `409 Conflict`. Add a `GET /devices` endpoint to list all registered devices.

---

## Files to create

### `SensorIngestApi/Models/Device.cs`

```csharp
namespace SensorIngestApi.Models
{
    public class Device
    {
        public string DeviceId { get; set; } = null!; // EF Core sets this; null! because nullable is enabled
        public DateTimeOffset RegisteredAtUtc { get; set; }
    }
}
```

- `DeviceId` is the natural primary key — no surrogate `long Id` needed here.
- `null!` is required because `<Nullable>enable</Nullable>` is set in the project; follow the same pattern used in `Alert.cs` and `SensorReading.cs`.

---

### `SensorIngestApi/Controllers/DevicesController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SensorIngestApi.Data;
using SensorIngestApi.Models;

namespace SensorIngestApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly TelemetryDbContext _db;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(TelemetryDbContext db, ILogger<DevicesController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterDeviceRequest request, CancellationToken ct)
        {
            if (await _db.Devices.AnyAsync(d => d.DeviceId == request.DeviceId, ct))
            {
                _logger.LogWarning("Duplicate registration attempt for {DeviceId}", request.DeviceId);
                return Conflict($"Device '{request.DeviceId}' is already registered.");
            }

            var device = new Device
            {
                DeviceId = request.DeviceId,
                RegisteredAtUtc = DateTimeOffset.UtcNow
            };

            _db.Devices.Add(device);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Registered device {DeviceId} at {RegisteredAtUtc}", device.DeviceId, device.RegisteredAtUtc);
            return CreatedAtAction(nameof(GetById), new { deviceId = device.DeviceId }, device);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var devices = await _db.Devices
                .OrderBy(d => d.DeviceId)
                .ToListAsync(ct);

            _logger.LogInformation("Returning {Count} registered devices", devices.Count);
            return Ok(devices);
        }

        [HttpGet("{deviceId}")]
        public async Task<IActionResult> GetById(string deviceId, CancellationToken ct)
        {
            var device = await _db.Devices.FindAsync(new object[] { deviceId }, ct);
            if (device is null)
            {
                _logger.LogWarning("Device not found: {DeviceId}", deviceId);
                return NotFound();
            }
            return Ok(device);
        }
    }

    public record RegisterDeviceRequest(string DeviceId);
}
```

---

## Files to modify

### `SensorIngestApi/Data/TelemetryDbContext.cs`

Add a `DbSet<Device>` property and configure the entity in `OnModelCreating`.

**Add after the existing `DbSet` properties:**
```csharp
public DbSet<Device> Devices => Set<Device>();
```

**Add inside `OnModelCreating`, after the `Alert` entity block:**
```csharp
b.Entity<Device>(e =>
{
    e.ToTable("devices");
    e.HasKey(x => x.DeviceId);
    e.Property(x => x.DeviceId).HasColumnName("device_id").HasMaxLength(128);
    e.Property(x => x.RegisteredAtUtc).HasColumnName("registered_at").HasColumnType("timestamptz");
});
```

The full file after edits should look like this:

```csharp
using Microsoft.EntityFrameworkCore;
using SensorIngestApi.Models;

namespace SensorIngestApi.Data
{
    public class TelemetryDbContext : DbContext
    {
        public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : base(options) { }

        public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
        public DbSet<Alert> Alerts => Set<Alert>();
        public DbSet<Device> Devices => Set<Device>();

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

            b.Entity<Device>(e =>
            {
                e.ToTable("devices");
                e.HasKey(x => x.DeviceId);
                e.Property(x => x.DeviceId).HasColumnName("device_id").HasMaxLength(128);
                e.Property(x => x.RegisteredAtUtc).HasColumnName("registered_at").HasColumnType("timestamptz");
            });
        }
    }
}
```

---

## EF Core migration

After modifying the DbContext, generate a migration:

```bash
dotnet ef migrations add AddDevices --project SensorIngestApi/SensorIngestApi.csproj
```

Then apply it:

```bash
dotnet ef database update --project SensorIngestApi/SensorIngestApi.csproj
```

The migration must only create a `devices` table — do not alter any existing tables or indices.

---

## Conventions to follow

These are non-negotiable for this project:

| Convention | Rule |
|---|---|
| Namespaces | File-scoped (`namespace Foo;` style is also fine, but match existing files which use block-style `namespace Foo { }`) |
| One type per file | `Device.cs` for the model. `RegisterDeviceRequest` is a `record` and may live in the same file as `DevicesController.cs` since it is only used there. |
| Async | All DB calls use `async`/`await` with `CancellationToken` forwarded. Never `.Result` or `.Wait()`. |
| Logging | `ILogger<T>` only. Structured templates with named properties: `{DeviceId}`, `{Count}`, etc. Never `Console.WriteLine`. |
| Log levels | `LogInformation` for successful registrations and list returns. `LogWarning` for duplicate attempts and not-found lookups. |
| Controller returns | `IActionResult` (not `TypedResults` — this is a controller-based project, not Minimal API). |
| Nullable | `<Nullable>enable</Nullable>` is active. Use `null!` on required string EF Core properties and add the comment shown above. |
| Private fields | `_` prefix + camelCase: `_db`, `_logger`. |
| No new packages | `Microsoft.EntityFrameworkCore` and `Npgsql.EntityFrameworkCore.PostgreSQL` are already in the project. No new NuGet references needed. |

---

## Verification

After implementing, confirm the build is clean:

```bash
dotnet build SensorIngestApi/SensorIngestApi.csproj
```

No warnings, no errors. The migration file must be committed alongside the model and controller changes.

---

## What NOT to do

- Do not add a repository pattern or service layer — `TelemetryDbContext` is injected directly into controllers, consistent with the rest of the project.
- Do not add any new NuGet packages.
- Do not modify `ReadingsController`, `StatsController`, `AlertsController`, or any existing migrations.
- Do not use `Thread.Sleep` — use `Task.Delay` with `CancellationToken` if any delay is needed.
- Do not add comments explaining what code does — only add a comment when the WHY is non-obvious (e.g., the `null!` explanation above).
