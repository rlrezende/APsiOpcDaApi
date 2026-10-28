using APsiC.Operations;
using APsiOpcDaApi.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace APsiOpcDaApi.Application.Infrastructure.HostedServices;
public sealed class OpcMonitorBackgroundService(IServiceProvider services,ILogger<OpcMonitorBackgroundService> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var instances=new Dictionary<Guid,(IServiceScope Scope,IOpcMonitoringService Monitor)>();
        try
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var registryScope=services.CreateScope();
                    var registry=registryScope.ServiceProvider.GetRequiredService<IWorkerOperationAccess>();
                    foreach(var id in registry.ConfiguredServers("communication.opcda"))
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        try
                        {
                            using var access=registry.Enter(id);
                            if(!instances.TryGetValue(id,out var instance))
                            {
                                var scope=services.CreateScope();
                                try{instance=(scope,scope.ServiceProvider.GetRequiredService<IOpcMonitoringService>());instances.Add(id,instance);}
                                catch{scope.Dispose();throw;}
                            }
                            await instance.Monitor.MonitorarTagsAsync(stoppingToken);
                        }
                        catch(OperationAdmissionDeniedException ex){logger.LogWarning("OPC worker denied for {ServerId}: {Code}",id,ex.Code);}
                    }
                    // Revocation stops new acquisition. It never triggers a physical disconnect.
                    await Task.Delay(TimeSpan.FromSeconds(30),stoppingToken);
                }
                catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}
                catch(Exception ex)
                {
                    logger.LogError(ex,"OPC worker cycle failed");
                    await Task.Delay(TimeSpan.FromSeconds(10),stoppingToken);
                }
            }
        }
        finally
        {
            // Host shutdown is an explicit lifecycle event, independent of licensing.
            foreach(var instance in instances.Values)
            {
                try{await instance.Monitor.StopMonitoringAsync();}
                catch(Exception ex){logger.LogError(ex,"OPC shutdown failed");}
                finally{instance.Scope.Dispose();}
            }
        }
    }
}
