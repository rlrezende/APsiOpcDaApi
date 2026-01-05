namespace APsiOpcDaApi.Application.Interfaces
{
    public interface IOpcMonitoringService
    {
        Task MonitorarTagsAsync(CancellationToken stoppingToken);
        Task StopMonitoringAsync();
    }
}

