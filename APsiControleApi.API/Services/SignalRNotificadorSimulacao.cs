using APsiControleApi.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using APsiControleApi.API.Hubs;

namespace APsiControleApi.API.Services
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
    }
}
