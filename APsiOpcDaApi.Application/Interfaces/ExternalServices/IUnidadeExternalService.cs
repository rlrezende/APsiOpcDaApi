using APsiOpcDaApi.Application.DTOs;

namespace APsiOpcDaApi.Application.Interfaces
{
    public interface IUnidadeExternalService
    {
        Task<Guid> CriarUnidadeAsync(UnidadeDto unidadeDto);
    }
}

