using APsiControleApi.Application.Interfaces;
using APsiControleApi.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace APsiControleApi.Application.Infrastructure.HostedServices
{
    public class DatabaseMonitorBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseMonitorBackgroundService> _logger;

        public DatabaseMonitorBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<DatabaseMonitorBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Database Monitor Background Service iniciado");

            using var scope = _serviceProvider.CreateScope();
            var databaseMonitoringService = scope.ServiceProvider.GetRequiredService<IDatabaseMonitoringService>();

            var opcServerService = scope.ServiceProvider.GetRequiredService<IOpcServerService>();
            var databaseServers = await opcServerService.GetServersByTypeAsync(Domain.Enum.TipoOpcServer.Database);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    foreach (var server in databaseServers)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            break;

                        await databaseMonitoringService.MonitorarTagsAsync(server.Id, stoppingToken);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Database Monitor Background Service cancelado");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no Database Monitor Background Service");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }


        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Parando Database Monitor Background Service...");
            await base.StopAsync(cancellationToken);
        }
    }
}
