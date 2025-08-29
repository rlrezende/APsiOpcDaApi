using APsiControleApi.Domain.Entities;
using APsiControleApi.Infrastructure.Configurations;
using APsiControleApi.Infrastructure.Seeders;
using Microsoft.EntityFrameworkCore;

namespace APsiControleApi.Infrastructure
{
    public class APsiControleApiContext : DbContext
    {
        public APsiControleApiContext(DbContextOptions<APsiControleApiContext> options) : base(options) { }

        public DbSet<Tag> Tag { get; set; }
        public DbSet<Controle> Controle { get; set; }
        public DbSet<Leitura> Leitura { get; set; }

        public DbSet<OpcServer> OpcServers { get; set; }
        public DbSet<OpcNode> OpcNodes { get; set; }
        public DbSet<OpcGroup> OpcGroups { get; set; }
        public DbSet<OpcDiscoveredServer> OpcDiscoveredServers { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Definir o schema padrão
            modelBuilder.HasDefaultSchema("APsiCDb");

            base.OnModelCreating(modelBuilder);

            // Aplicar configurações específicas para cada entidade
            modelBuilder.ApplyConfiguration(new ControleConfiguration());
            modelBuilder.ApplyConfiguration(new LeituraConfiguration());
            modelBuilder.ApplyConfiguration(new TagConfiguration());
            modelBuilder.ApplyConfiguration(new OpcNodeConfiguration());
            modelBuilder.ApplyConfiguration(new OpcServerConfiguration());
            modelBuilder.ApplyConfiguration(new OpcGroupConfiguration());
            modelBuilder.ApplyConfiguration(new OpcDiscoveredServerConfiguration());

            // Aplicar Seeders
            ModuloSeeder.Seed(modelBuilder);
            // Adicione outros seeders conforme necessário
        }
    }
}
