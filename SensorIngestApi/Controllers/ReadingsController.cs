using Microsoft.AspNetCore.Mvc;
using SensorIngestApi.Models;
using System.Threading.Channels;

namespace SensorIngestApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReadingsController : ControllerBase
    {
        private readonly Channel<SensorReading> _channel;
        private readonly ILogger<ReadingsController> _logger;

        public ReadingsController(Channel<SensorReading> channel, ILogger<ReadingsController> logger)
        {
            _channel = channel;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SensorReading reading)
        {
            if (!_channel.Writer.TryWrite(reading))
            {
                if (!await _channel.Writer.WaitToWriteAsync())
                {
                    _logger.LogWarning("Channel full, dropping data");
                    return StatusCode(503);
                }
                _channel.Writer.TryWrite(reading);
            }
            _logger.LogDebug("Accepted reading from {DeviceId} at {Ts}", reading.DeviceId, reading.TimestampUtc);
            return Accepted();
        }
    }
}
