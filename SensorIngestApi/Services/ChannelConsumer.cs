using Microsoft.AspNetCore.SignalR;
using SensorIngestApi.Hubs;
using SensorIngestApi.Interfaces;
using SensorIngestApi.Models;
using System.Threading.Channels;

namespace SensorIngestApi.Services
{
    public class ChannelConsumer : BackgroundService
    {
        private readonly Channel<SensorReading> _channel;
        private readonly IThroughputStats _stats;
        private readonly IAggregator _aggr;
        private readonly IAlertBus _alerts;
        private readonly IHubContext<TelemetryHub> _hub;
        private readonly int _parallel;
        private readonly ILogger<ChannelConsumer> _logger;

        public ChannelConsumer(
            Channel<SensorReading> channel,
            IThroughputStats stats,
            IAggregator aggr,
            IAlertBus alerts,
            IHubContext<TelemetryHub> hub,
            int parallelConsumers = 4,
            ILogger<ChannelConsumer> logger = null)
        {
            _channel = channel;
            _stats = stats;
            _aggr = aggr;
            _alerts = alerts;
            _hub = hub;
            _parallel = Math.Max(1, parallelConsumers);
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // N workers draining the channel
            var workers = Enumerable.Range(0, _parallel).Select(_ => Task.Run(async () =>
            {
                var reader = _channel.Reader;
                while (await reader.WaitToReadAsync(stoppingToken))
                {
                    while (reader.TryRead(out var r))
                    {
                        _aggr.Add(r);
                        _stats.MarkOne();

                        // Anomaly rule 1: absolute threshold (adjust for your domain)
                        if (r.Value < 0 || r.Value > 120)
                            Publish(new Alert
                            {
                                DeviceId = r.DeviceId,
                                Utc = DateTime.UtcNow,
                                Value = r.Value,
                                Kind = "Threshold",
                                Message = $"Value {r.Value} out of [0,120]"
                            });

                        // Anomaly rule 2: z-score >= 3 (per-device rolling baseline)
                        var (mean, std, n) = _aggr.Baseline(r.DeviceId);
                        if (n > 20 && std > 0)
                        {
                            var z = Math.Abs((r.Value - mean) / std);
                            if (z >= 3)
                                Publish(new Alert
                                {
                                    DeviceId = r.DeviceId,
                                    Utc = DateTime.UtcNow,
                                    Value = r.Value,
                                    Kind = "Anomaly",
                                    Message = $"z={z:0.00} (μ={mean:0.00}, σ={std:0.00})"
                                });
                        }
                    }

                    _stats.EstimatedQueueLength = reader.Count;
                }
            }, stoppingToken)).ToArray();

            // Emit live rate every second
            _ = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var perSec = _stats.GetPerSecond();
                    _aggr.TrackPerSecond(DateTime.UtcNow, perSec);

                    await _hub.Clients.All.SendAsync("stats", new
                    {
                        utc = DateTime.UtcNow,
                        perSecond = perSec,
                        total = _stats.TotalProcessed
                    }, cancellationToken: stoppingToken);

                    _logger.LogWarning("Backpressure rising: queue length {QueueLength}", perSec);

                    await Task.Delay(1000, stoppingToken);
                }
            }, stoppingToken);

            return Task.WhenAll(workers);
        }

        private void Publish(Alert a)
        {
            _alerts.Publish(a);
            _hub.Clients.All.SendAsync("alert", a);
            _logger.LogWarning("Anomaly {Kind} for {DeviceId} value {Value} at {Utc}",
                a.Kind, a.DeviceId, a.Value, a.Utc);
        }
    }
}
