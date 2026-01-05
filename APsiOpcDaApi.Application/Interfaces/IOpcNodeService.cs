using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Domain.Entities;

namespace APsiOpcDaApi.Application.Interfaces
{
    public interface IOpcNodeService : IGenericService<OpcNode, OpcNodeDTO>
    {
    }
}

