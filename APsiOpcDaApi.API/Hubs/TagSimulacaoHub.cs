using Microsoft.AspNetCore.SignalR;

namespace APsiOpcDaApi.API.Hubs
{
    public class TagSimulacaoHub(APsiC.Licensing.AspNetCore.RealtimeConnections connections) : APsiC.Licensing.AspNetCore.CanonicalRealtimeHub(connections)
    {
    }
}

