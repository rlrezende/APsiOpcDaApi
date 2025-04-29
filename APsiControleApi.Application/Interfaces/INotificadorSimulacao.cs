namespace APsiControleApi.Application.Interfaces
{
    public interface INotificadorSimulacao
    {
        Task NotificarSimulacaoAsync(IEnumerable<object> dados);
    }
}
