namespace APsiControleApi.Infrastructure.Configurations
{
    using APsiControleApi.Domain.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.ToTable("Tag");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Nome)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(t => t.Descricao)
                   .IsRequired()
                   .HasMaxLength(255); // Ajustei para um tamanho mais realista

            // Relacionamento com Unidade (muitas Tags para uma Unidade)
            builder.HasOne(t => t.Unidade)
                   .WithMany(u => u.Tags) // Assumindo que Unidade tem uma coleção de Tags
                   .HasForeignKey(t => t.UnidadeId)
                   .OnDelete(DeleteBehavior.Restrict); // Evita exclusão em cascata

            // Relacionamento com Leituras (Uma Tag pode ter muitas Leituras)
            builder.HasMany(t => t.Leituras)
                   .WithOne(l => l.Tag) // Assumindo que Leitura tem uma propriedade Tag
                   .HasForeignKey(l => l.TagId)
                   .OnDelete(DeleteBehavior.Cascade); // Deleta leituras ao remover uma Tag
        }
    }
}
