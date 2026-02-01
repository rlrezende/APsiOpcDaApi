using APsiOpcDaApi.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace APsiOpcDaApi.Application.Infrastructure.HostedServices
{
    public class OpcMonitorBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OpcMonitorBackgroundService> _logger;

        public OpcMonitorBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<OpcMonitorBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 OPC Monitor Background Service iniciado");
            

             using var scope = _serviceProvider.CreateScope();
             var monitoringService = scope.ServiceProvider.GetRequiredService<IOpcMonitoringService>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("🔄 Iniciando ciclo de monitoramento...");

                    await monitoringService.MonitorarTagsAsync(stoppingToken);

                    _logger.LogInformation("✅ Ciclo de monitoramento concluído");
                    
                    // Aguardar antes da próxima verificação (30 segundos)
                    await Task.Delay(30000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("🛑 OPC Monitor Background Service cancelado");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no OPC Monitor Background Service");
                    await Task.Delay(10000, stoppingToken); // Aguardar 10s antes de tentar novamente
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Parando OPC Monitor Background Service...");
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var monitoringService = scope.ServiceProvider.GetRequiredService<IOpcMonitoringService>();
                await monitoringService.StopMonitoringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao parar monitoramento OPC");
            }

            await base.StopAsync(cancellationToken);
        }
    }
}

