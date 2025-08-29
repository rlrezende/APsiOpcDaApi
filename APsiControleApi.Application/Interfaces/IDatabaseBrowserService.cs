using APsiControleApi.Application.DTOs;

namespace APsiControleApi.Application.Interfaces
{
    public interface IDatabaseBrowserService
    {
        Task<OpcBrowseResultDTO> BrowseNodesAsync(Guid serverId, string? parentNodeId = null);
        Task<string?> ObterValorColunaAsync(Guid opcServerId, string nomeTabela, string nomeColuna);
    }

}
