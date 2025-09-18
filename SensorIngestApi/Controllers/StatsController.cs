using Microsoft.AspNetCore.Mvc;
using SensorIngestApi.Interfaces;
using SensorIngestApi.Models;

namespace SensorIngestApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly IThroughputStats _stats;
        private readonly IAggregator _aggr;

        public StatsController(IThroughputStats stats, IAggregator aggr)
        {
            _stats = stats;
            _aggr = aggr;
        }

        [HttpGet]
        public ActionResult<StatsDto> Get()
        {
            var dto = new StatsDto
            {
                TotalProcessed = _stats.TotalProcessed,
                PerSecond = _stats.GetPerSecond(),
                QueueLength = _stats.EstimatedQueueLength,
                Global = _aggr.GetGlobal(),
                TopDevices = _aggr.GetTopDevices(10)
            };
            return Ok(dto);
        }
    }
}
