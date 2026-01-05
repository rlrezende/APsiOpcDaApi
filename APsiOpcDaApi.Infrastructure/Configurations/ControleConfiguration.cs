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

            // Configuração da propriedade UnidadeId como chave estrangeira, sem referência de navegação
            builder.Property(c => c.UnidadeId)
                   .IsRequired();

            // Caso você queira definir um índice para UnidadeId
            builder.HasIndex(c => c.UnidadeId)
                   .HasDatabaseName("IX_Controle_UnidadeId");
        }
    }
}

