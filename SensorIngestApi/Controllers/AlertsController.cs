using Microsoft.AspNetCore.Mvc;
using SensorIngestApi.Interfaces;
using SensorIngestApi.Models;

namespace SensorIngestApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AlertsController : ControllerBase
    {
        private readonly IAlertBus _bus;
        private readonly ILogger<AlertsController> _logger;

        public AlertsController(IAlertBus bus, ILogger<AlertsController> logger)
        {
            _bus = bus;
            _logger = logger;
        }


        [HttpGet("recent")]
        public ActionResult<IEnumerable<Alert>> Recent()
        {
            // scope helps correlate all logs in this action
            using var _ = _logger.BeginScope("GET /alerts/recent");

            var arr = _bus.GetRecent().ToArray();
            var oldestUtc = arr.Length > 0 ? arr.First().Utc : (DateTime?)null;
            var newestUtc = arr.Length > 0 ? arr.Last().Utc : (DateTime?)null;

            _logger.LogInformation(
                "Returning {Count} recent alerts (OldestUtc={OldestUtc}, NewestUtc={NewestUtc})",
                arr.Length, oldestUtc, newestUtc);

            return Ok(arr);
        }
    }
}
