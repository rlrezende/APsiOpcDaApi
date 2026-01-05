using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APsiOpcDaApi.Application.Services
{
    public class DatabaseMonitoringService : IDatabaseMonitoringService
    {
        private readonly ITagService _tagService;
        private readonly IDatabaseBrowserService _databaseBrowserService;
        private readonly ILeituraService _leituraService;
        private readonly INotificadorSimulacao _notificador;
        private readonly IOpcGroupService _groupService;
        private readonly ILogger<DatabaseMonitoringService> _logger;

        public DatabaseMonitoringService(
            ITagService tagService,
            IDatabaseBrowserService databaseBrowserService,
            ILeituraService leituraService,
            INotificadorSimulacao notificador,
            IOpcGroupService groupService,
            ILogger<DatabaseMonitoringService> logger)
        {
            _tagService = tagService;
            _databaseBrowserService = databaseBrowserService;
            _leituraService = leituraService;
            _notificador = notificador;
            _groupService = groupService;
            _logger = logger;
        }

        public async Task MonitorarTagsAsync(Guid serverId, CancellationToken cancellationToken)
        {
            try
            {
                var tags = await _tagService.GetTagsByServerAsync(serverId, "Database");
                var grupos = tags.Where(t => t.GroupId.HasValue).GroupBy(t => t.GroupId.Value);

                foreach (var grupo in grupos)
                {
                    var groupConfig = await _groupService.GetByIdAsync(grupo.Key);
                    if (groupConfig == null || !groupConfig.IsActive)
                        continue;

                    foreach (var tag in grupo)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        if (!tag.Monitora || string.IsNullOrEmpty(tag.NomeTabela) || string.IsNullOrEmpty(tag.NomeColuna))
                            continue;

                        var valor = await _databaseBrowserService.ObterValorColunaAsync(serverId, tag.NomeTabela, tag.NomeColuna);

                        if (double.TryParse(valor, out var valorNumerico))
                        {
                            tag.ValorAtual = valorNumerico;
                            await _tagService.UpdateAsync(tag);

                            await _leituraService.AddAsync(new LeituraDTO
                            {
                                TagId = tag.Id,
                                Valor = valorNumerico,
                                DataLeitura = DateTime.UtcNow
                            });

                            await _notificador.NotificarAtualizacaoTagAsync(
                                tag.Id,
                                valorNumerico,
                                DateTime.UtcNow);
                        }

                        await Task.Delay(groupConfig.UpdateRate, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao monitorar tags do servidor {serverId}");
            }
        }
    }

    public interface IDatabaseMonitoringService
    {
        Task MonitorarTagsAsync(Guid serverId, CancellationToken cancellationToken);
    }

}

