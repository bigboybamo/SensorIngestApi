using SensorIngestApi.Models;
using System.Threading.Channels;

namespace SensorIngestApi.Services
{
    public class SensorSimulator : BackgroundService
    {
        private readonly Channel<SensorReading> _channel;
        private readonly int _ratePerSecond;
        private readonly bool _enabled;

        public SensorSimulator(Channel<SensorReading> channel, int ratePerSecond, bool enabled = true)
        {
            _channel = channel;
            _ratePerSecond = ratePerSecond;
            _enabled = enabled;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_enabled) return;

            const int tickMs = 10; // 100 ticks/sec
            var perBatch = Math.Max(1, _ratePerSecond / (1000 / tickMs));
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(tickMs));
            var rnd = new Random();

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                for (int i = 0; i < perBatch; i++)
                {
                    var reading = new SensorReading
                    {
                        DeviceId = $"dev-{rnd.Next(1, 100):D3}",
                        TimestampUtc = DateTimeOffset.UtcNow,
                        Value = Math.Round(rnd.NextDouble() * 100, 3)
                    };

                    if (!_channel.Writer.TryWrite(reading))
                    {
                        if (!await _channel.Writer.WaitToWriteAsync(stoppingToken)) return;
                        _channel.Writer.TryWrite(reading);
                    }
                }
            }
        }
    }
}
