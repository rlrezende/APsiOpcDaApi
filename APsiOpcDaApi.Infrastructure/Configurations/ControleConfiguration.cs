namespace APsiOpcDaApi.Infrastructure.Configurations
{
    using APsiOpcDaApi.Domain.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class ControleConfiguration : IEntityTypeConfiguration<Controle>
    {
        public void Configure(EntityTypeBuilder<Controle> builder)
        {
            // Nome da tabela
            builder.ToTable("Controle");

            // Definição da chave primária
            builder.HasKey(c => c.Id);

            // Configurações das propriedades
            builder.Property(c => c.Nome)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(c => c.Descricao)
                   .IsRequired()
                   .HasMaxLength(255);

            // Configuração da propriedade ModuloId como chave estrangeira, sem referência de navegação
            builder.Property(c => c.ModuloId)
                   .IsRequired();

            // Índice em ModuloId para melhorar desempenho de consultas
            builder.HasIndex(c => c.ModuloId)
                   .HasDatabaseName("IX_Controle_ModuloId");
        }
    }
}

