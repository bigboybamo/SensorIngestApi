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

        public AlertsController(IAlertBus bus) => _bus = bus;

        [HttpGet("recent")]
        public ActionResult<IEnumerable<Alert>> Recent() => Ok(_bus.GetRecent());
    }
}
