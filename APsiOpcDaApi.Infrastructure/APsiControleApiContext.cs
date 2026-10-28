using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Infrastructure.Configurations;
using APsiOpcDaApi.Infrastructure.Seeders;
using Microsoft.EntityFrameworkCore;

namespace APsiOpcDaApi.Infrastructure
{
    public class APsiOpcDaApiContext : DbContext
    {
        private readonly IQuotaSaveBoundary? _quota;
        public APsiOpcDaApiContext(DbContextOptions<APsiOpcDaApiContext> options, IQuotaSaveBoundary? quota = null) : base(options) { _quota = quota; }
        public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
            _quota is null ? base.SaveChanges(acceptAllChangesOnSuccess) :
                _quota.Save(this, () => base.SaveChanges(acceptAllChangesOnSuccess));
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            if (_quota is null) return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_quota.Save(this, () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = base.SaveChanges(acceptAllChangesOnSuccess);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }));
        }

        public DbSet<Tag> Tag { get; set; }
        public DbSet<Controle> Controle { get; set; }
        public DbSet<Leitura> Leitura { get; set; }

        public DbSet<OpcServer> OpcServers { get; set; }
        public DbSet<OpcNode> OpcNodes { get; set; }
        public DbSet<OpcGroup> OpcGroups { get; set; }
        public DbSet<OpcDiscoveredServer> OpcDiscoveredServers { get; set; }

        public DbSet<DetectModel> DetectModels { get; set; }
        public DbSet<DetectModelTag> DetectModelTags { get; set; }
        public DbSet<DetectModelPipeline> DetectModelPipelines { get; set; }
        public DbSet<DetectTrainingJob> DetectTrainingJobs { get; set; }

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
            modelBuilder.ApplyConfiguration(new DetectModelConfiguration());
            modelBuilder.ApplyConfiguration(new DetectModelTagConfiguration());
            modelBuilder.ApplyConfiguration(new DetectModelPipelineConfiguration());
            modelBuilder.ApplyConfiguration(new DetectTrainingJobConfiguration());

            // Aplicar Seeders
            ModuloSeeder.Seed(modelBuilder);
            // Adicione outros seeders conforme necessário
        }
    }
}

