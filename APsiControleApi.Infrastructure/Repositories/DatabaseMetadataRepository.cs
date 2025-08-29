using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;
using APsiControleApi.Domain.Interfaces.Repositories;

namespace APsiControleApi.Infrastructure.Repositories
{
    public class DatabaseMetadataRepository : IDatabaseMetadataRepository
    {
        public async Task<List<string>> ObterTabelasAsync(string provider, string connectionString)
        {
            var tabelas = new List<string>();
            using var connection = CriarConexao(provider, connectionString);
            await connection.OpenAsync();

            if (provider.ToLower() == "postgresql")
            {
                var schemaName = ExtrairSearchPath(connectionString) ?? "public";

                // [catalog, schema, table, type]
                string[] restrictions = new string[4];
                restrictions[1] = schemaName;

                var schema = connection.GetSchema("Tables", restrictions);
                foreach (DataRow row in schema.Rows)
                {
                    var tableName = row["TABLE_NAME"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(tableName))
                        tabelas.Add(tableName);
                }
            }
            else
            {
                var schema = connection.GetSchema("Tables");
                foreach (DataRow row in schema.Rows)
                {
                    var tableName = row["TABLE_NAME"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(tableName))
                        tabelas.Add(tableName);
                }
            }

            return tabelas;
        }

        public async Task<List<(string NomeColuna, string Tipo)>> ObterColunasAsync(string provider, string connectionString, string nomeTabela)
        {
            var colunas = new List<(string NomeColuna, string Tipo)>();
            using var connection = CriarConexao(provider, connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM \"{nomeTabela}\" WHERE 1=0"; // Para pegar apenas metadados
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SchemaOnly);

            var schemaTable = reader.GetSchemaTable();
            foreach (DataRow row in schemaTable.Rows)
            {
                var nome = row["ColumnName"]?.ToString() ?? "";
                var tipo = row["DataTypeName"]?.ToString() ?? row["DataType"]?.ToString() ?? "";
                colunas.Add((nome, tipo));
            }

            return colunas;
        }

        public async Task<string?> ObterValorColunaAsync(string provider, string connectionString, string nomeTabela, string nomeColuna)
        {
            string query;

            if (provider.ToLower() == "sqlserver")
                query = $"SELECT TOP 1 [{nomeColuna}] FROM [{nomeTabela}] ORDER BY 1 DESC";
            else if (provider.ToLower() == "postgresql")
                query = $"SELECT \"{nomeColuna}\" FROM \"{nomeTabela}\" ORDER BY 1 DESC LIMIT 1";
            else
                throw new NotSupportedException($"Provider '{provider}' não suportado.");

            using var connection = CriarConexao(provider, connectionString);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = query;

            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }

        private DbConnection CriarConexao(string provider, string connectionString)
        {
            return provider.ToLower() switch
            {
                "sqlserver" => new SqlConnection(connectionString),
                "postgresql" => new NpgsqlConnection(connectionString),
                _ => throw new NotSupportedException($"Provider não suportado: {provider}")
            };
        }

        private string? ExtrairSearchPath(string connectionString)
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                if (part.Trim().StartsWith("Search Path=", StringComparison.OrdinalIgnoreCase))
                {
                    return part.Split('=')[1].Trim();
                }
            }

            return null;
        }
    }
}
