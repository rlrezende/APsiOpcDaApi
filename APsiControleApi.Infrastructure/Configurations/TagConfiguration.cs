namespace APsiControleApi.Infrastructure.Configurations
{
    using APsiControleApi.Domain.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            // Nome da tabela
            builder.ToTable("Tag");

            // Definição da chave primária
            builder.HasKey(t => t.Id);

            // Configurações das propriedades
            builder.Property(t => t.Nome)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(t => t.Descricao)
                   .IsRequired()
                   .HasMaxLength(255);

            // Configuração da propriedade UnidadeId (sem referência direta à Unidade)
            builder.Property(t => t.UnidadeId)
                   .IsRequired();

             builder.Property(t => t.idOld)
                   .IsRequired();

            // Opcional: índice em UnidadeId para melhorar desempenho de consultas
            builder.HasIndex(t => t.UnidadeId)
                   .HasDatabaseName("IX_Tag_UnidadeId");

            // Relacionamento com Leituras (Uma Tag pode ter muitas Leituras)
            builder.HasMany(t => t.Leituras)
                   .WithOne(l => l.Tag)
                   .HasForeignKey(l => l.TagId)
                   .OnDelete(DeleteBehavior.Cascade); // Deleta leituras ao remover uma Tag
        }
    }
}
