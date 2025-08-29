namespace APsiControleApi.Application.Interfaces
{
    public interface INotificadorSimulacao
    {
        Task NotificarSimulacaoAsync(IEnumerable<object> dados);
        Task NotificarAtualizacaoTagAsync(Guid tagId, double valor, DateTime dataLeitura);
    }
}
