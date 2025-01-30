namespace APsiControleApi.Domain.Interfaces.ExternalServices
{
    public interface IUnidadeExternalService
    {
        Task<Guid> CriarUnidadeAsync(UnidadeDto unidadeDto);
    }
}
