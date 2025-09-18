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

        public ReadingsController(Channel<SensorReading> channel)
        {
            _channel = channel;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SensorReading reading)
        {
            if (!_channel.Writer.TryWrite(reading))
            {
                if (!await _channel.Writer.WaitToWriteAsync())
                    return StatusCode(503);
                _channel.Writer.TryWrite(reading);
            }
            return Accepted();
        }
    }
}
