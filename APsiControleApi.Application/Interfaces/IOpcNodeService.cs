using APsiControleApi.Application.DTOs;
using APsiControleApi.Domain.Entities;

namespace APsiControleApi.Application.Interfaces
{
    public interface IOpcNodeService : IGenericService<OpcNode, OpcNodeDTO>
    {
    }
}
