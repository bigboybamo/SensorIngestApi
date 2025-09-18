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
        private readonly ILogger<StatsController> _logger;

        public StatsController(IThroughputStats stats, IAggregator aggr, ILogger<StatsController> logger)
        {
            _stats = stats;
            _aggr = aggr;
            _logger = logger;
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

            _logger.LogInformation(
                "Stats snapshot TotalProcessed={TotalProcessed} PerSecond={PerSecond} Queue={Queue} TopDevices={TopCount}",
                dto.TotalProcessed, dto.PerSecond, dto.QueueLength, dto.TopDevices.Count);

            return Ok(dto);
        }
    }
}
