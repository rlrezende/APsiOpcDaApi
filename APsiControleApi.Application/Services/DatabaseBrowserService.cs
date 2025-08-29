using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Interfaces.Repositories;

namespace APsiControleApi.Application.Services
{
    public class DatabaseBrowserService : IDatabaseBrowserService
    {
        private readonly IGenericRepository<OpcServer> _opcServerRepository;
        private readonly IDatabaseMetadataRepository _metadataRepository;

        public DatabaseBrowserService(
            IGenericRepository<OpcServer> opcServerRepository,
            IDatabaseMetadataRepository metadataRepository)
        {
            _opcServerRepository = opcServerRepository;
            _metadataRepository = metadataRepository;
        }
        
        public async Task<OpcBrowseResultDTO> BrowseNodesAsync(Guid serverId, string? parentNodeId = null)
        {
            var server = await _opcServerRepository.GetByIdAsync(serverId);

            if (server == null)
                throw new ArgumentException("Servidor não encontrado.");

            if (server.Tipo != Domain.Enum.TipoOpcServer.Database)
                throw new InvalidOperationException("Tipo de servidor não é Database.");

            var result = new OpcBrowseResultDTO();

            // Se parentNodeId for nulo, estamos listando as tabelas (nível raiz)
            if (string.IsNullOrEmpty(parentNodeId))
            {
                var tabelas = await _metadataRepository.ObterTabelasAsync(server.Provider!, server.ConnectionString!);

                result.Nodes = tabelas.Select(t => new OpcNodeBrowseDTO
                {
                    NodeId = t, // o nome da tabela é o NodeId neste contexto
                    DisplayName = t,
                    BrowseName = t,
                    NodeClass = "Object",
                    HasChildren = true,
                    Icon = "database"
                }).ToList();
            }
            else
            {
                // Caso parentNodeId tenha valor, estamos listando colunas da tabela correspondente
                var colunas = await _metadataRepository.ObterColunasAsync(server.Provider!, server.ConnectionString!, parentNodeId);

                result.Tags = colunas.Select(c => new OpcTagDTO
                {
                    NodeId = $"{parentNodeId}.{c.NomeColuna}",
                    DisplayName = c.NomeColuna,
                    BrowseName = c.NomeColuna,
                    DataType = c.Tipo,
                    NodeClass = "Variable",
                    AccessLevel = "Read",
                    Icon = "tag",
                    HasChildren = false
                }).ToList();
            }

            return result;
        }



        public async Task<string?> ObterValorColunaAsync(Guid opcServerId, string nomeTabela, string nomeColuna)
        {
            var server = await _opcServerRepository.GetByIdAsync(opcServerId);

            if (server == null)
                throw new ArgumentException("Servidor não encontrado.");

            if (server.Tipo != Domain.Enum.TipoOpcServer.Database)
                throw new InvalidOperationException("Tipo de servidor não é Database.");

            // Consulta o valor mais recente da coluna
            var valor = await _metadataRepository.ObterValorColunaAsync(
                server.Provider!,
                server.ConnectionString!,
                nomeTabela,
                nomeColuna);

            return valor;
        }
    }
}
