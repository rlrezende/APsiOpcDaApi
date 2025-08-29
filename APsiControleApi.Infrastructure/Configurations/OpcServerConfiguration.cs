using APsiControleApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace APsiControleApi.Infrastructure.Configurations
{
    public class OpcServerConfiguration : IEntityTypeConfiguration<OpcServer>
    {
              public void Configure(EntityTypeBuilder<OpcServer> builder)
              {
                     // Nome da tabela
                     builder.ToTable("OpcServer");

                     // Chave primária
                     builder.HasKey(s => s.Id);

                     // Nome obrigatório, até 100 caracteres
                     builder.Property(s => s.Nome)
                            .IsRequired()
                            .HasMaxLength(100);

                     // Endpoint obrigatório, até 255 caracteres
                     builder.Property(s => s.Endpoint)
                            .IsRequired()
                            .HasMaxLength(255);

                     // UnidadeId obrigatório (sem relação direta mapeada aqui, se não tiver entidade Unidade)
                     builder.Property(s => s.UnidadeId)
                            .IsRequired();

                     // Relacionamento com os Nodes
                     builder.HasMany(s => s.Nodes)
                            .WithOne(n => n.Server)
                            .HasForeignKey(n => n.ServerId)
                            .OnDelete(DeleteBehavior.Cascade); // Remove nodes se o servidor for deletado
                   

                     builder.Property(p => p.Tipo)
                            .HasConversion<int>()
                            .IsRequired();

                     builder.Property(p => p.Provider)
                            .HasMaxLength(50);

                     builder.Property(p => p.ConnectionString)
                            .HasMaxLength(1000);

        }
    }
}
