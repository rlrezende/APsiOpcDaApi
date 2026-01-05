using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.IO;

namespace APsiOpcDaApi.Infrastructure
{
    public class APsiOpcDaApiContextFactory : IDesignTimeDbContextFactory<APsiOpcDaApiContext>
    {
        public APsiOpcDaApiContext CreateDbContext(string[] args)
        {
            // Obtém a configuração a partir do arquivo appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<APsiOpcDaApiContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Configura o DbContext com o banco de dados usando o connection string
            // Inclui o uso de NodaTime com a configuração do Npgsql
            optionsBuilder.UseNpgsql(connectionString, options => options.UseNodaTime());

            return new APsiOpcDaApiContext(optionsBuilder.Options);
        }
    }
}

