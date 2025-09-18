namespace SensorIngestApi.Interfaces
{
    public interface IThroughputStats
    {
        long TotalProcessed { get; }
        int EstimatedQueueLength { get; set; }
        void MarkOne();
        double GetPerSecond();
    }
}
