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