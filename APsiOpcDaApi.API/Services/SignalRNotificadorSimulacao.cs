using APsiOpcDaApi.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using APsiOpcDaApi.API.Hubs;

namespace APsiOpcDaApi.API.Services
{
    public class SignalRNotificadorSimulacao : INotificadorSimulacao
    {
        private readonly IHubContext<TagSimulacaoHub> _hubContext;

        public SignalRNotificadorSimulacao(IHubContext<TagSimulacaoHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotificarSimulacaoAsync(IEnumerable<object> dados)
        {
            await _hubContext.Clients.All.SendAsync("ReceberSimulacao", dados);
        }

        public async Task NotificarAtualizacaoTagAsync(Guid tagId, double valor, DateTime dataLeitura)
        {
            var payload = new
            {
                tagId,
                valor,
                data = dataLeitura
            };

            await _hubContext.Clients.All.SendAsync("AtualizarTag", payload);
        }
    }
}

