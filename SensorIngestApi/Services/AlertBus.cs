using SensorIngestApi.Interfaces;
using SensorIngestApi.Models;
using System.Collections.Concurrent;

namespace SensorIngestApi.Services
{
    public class AlertBus : IAlertBus
    {
        private readonly ConcurrentQueue<Alert> _recent = new();
        private const int Keep = 500;

        public void Publish(Alert alert)
        {
            _recent.Enqueue(alert);
            while (_recent.Count > Keep) _recent.TryDequeue(out _);
        }

        public IEnumerable<Alert> GetRecent() => _recent.ToArray();
    }
}
