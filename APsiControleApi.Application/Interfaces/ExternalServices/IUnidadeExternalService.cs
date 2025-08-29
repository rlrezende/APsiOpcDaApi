using APsiControleApi.Application.DTOs;

namespace APsiControleApi.Application.Interfaces
{
    public interface IUnidadeExternalService
    {
        Task<Guid> CriarUnidadeAsync(UnidadeDto unidadeDto);
    }
}
