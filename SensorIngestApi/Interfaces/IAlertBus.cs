using SensorIngestApi.Models;

namespace SensorIngestApi.Interfaces
{
    public interface IAlertBus
    {
        void Publish(Alert alert);
        IEnumerable<Alert> GetRecent();
    }
}
