namespace APsiOpcDaApi.Infrastructure.Configurations
{
    using APsiOpcDaApi.Domain.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class TagNewConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            // Nome da tabela
            builder.ToTable("TagNew");

            // Definição da chave primária
            builder.HasKey(t => t.Id);

            // Configurações das propriedades
            builder.Property(t => t.Nome)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(t => t.Descricao)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(t => t.Descricao)
                   .IsRequired()
                   .HasMaxLength(255);

            // Configuração da propriedade ModuloId (sem referência direta ao Módulo)
            builder.Property(t => t.ModuloId)
                   .IsRequired();

            // Índice em ModuloId para melhorar desempenho de consultas
            builder.HasIndex(t => t.ModuloId)
                   .HasDatabaseName("IX_Tag_ModuloId");

            // Relacionamento com Leituras (Uma Tag pode ter muitas Leituras)
            builder.HasMany(t => t.Leituras)
                   .WithOne(l => l.Tag)
                   .HasForeignKey(l => l.TagId)
                   .OnDelete(DeleteBehavior.Cascade); // Deleta leituras ao remover uma Tag
        }
    }
}

