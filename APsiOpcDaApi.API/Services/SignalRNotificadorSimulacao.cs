using APsiC.Licensing.AspNetCore;
using APsiOpcDaApi.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using APsiOpcDaApi.API.Hubs;

namespace APsiOpcDaApi.API.Services
{
    public class SignalRNotificadorSimulacao : INotificadorSimulacao
    {
        private readonly IScopedRealtimeAudience _audience;
        private readonly IHubContext<TagSimulacaoHub> _hubContext;

        public SignalRNotificadorSimulacao(IHubContext<TagSimulacaoHub> hubContext, IScopedRealtimeAudience audience)
        {
            _audience = audience;
            _hubContext = hubContext;
        }

        public async Task NotificarSimulacaoAsync(IEnumerable<object> dados)
        {
            foreach(var delivery in RealtimeBatch.Partition(_audience,dados))
                await _hubContext.Clients.Clients(delivery.Connections).SendAsync("ReceberSimulacao",delivery.Items);
        }

        public async Task NotificarAtualizacaoTagAsync(Guid tagId, double valor, DateTime dataLeitura)
        {
            var payload = new
            {
                tagId,
                valor,
                data = dataLeitura
            };

            await _hubContext.Clients.Clients(_audience.Recipients("AtualizarTag",payload)).SendAsync("AtualizarTag", payload);
        }
    }
}

