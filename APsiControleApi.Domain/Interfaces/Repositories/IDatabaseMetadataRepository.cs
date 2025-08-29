namespace APsiControleApi.Domain.Interfaces.Repositories
{
    public interface IDatabaseMetadataRepository
    {
        Task<List<string>> ObterTabelasAsync(string provider, string connectionString);
        Task<List<(string NomeColuna, string Tipo)>> ObterColunasAsync(string provider, string connectionString, string nomeTabela);
         // ✅ Novo método
        Task<string?> ObterValorColunaAsync(string provider, string connectionString, string nomeTabela, string nomeColuna);
    }
}
