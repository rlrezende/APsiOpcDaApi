namespace APsiControleApi.Application.Interfaces
{
    public interface ISimuladorLeituraService
    {
        Task IniciarSimulacaoAsync(List<Guid> tagIds, Guid unidadeId);
        Task IniciarSimulacaoPIDComRespostaAoDegrauAsync(
            double k, double tau, double theta,
            Guid? tagKp = null,
            Guid? tagKi = null,
            Guid? tagKd = null,
            List<Guid>? outrasTags = null,
            Guid unidadeId = default,
            double? valorInicial = null);

        Task IniciarSimulacaoPIDOscilacaoSustentadaAsync(
            double ku, double pu,
            Guid? tagKp = null,
            Guid? tagKi = null,
            Guid? tagKd = null,
            Guid unidadeId = default);

        Task IniciarSimulacaoPIDSinteseDiretaAsync(
            double k, double tau, double theta, double taud,
            Guid? tagKp = null,
            Guid? tagKi = null,
            Guid? tagKd = null,
            Guid unidadeId = default);
    }
}
