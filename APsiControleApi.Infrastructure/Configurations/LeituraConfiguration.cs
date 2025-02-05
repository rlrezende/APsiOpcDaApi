using APsiControleApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace APsiControleApi.Infrastructure.Configurations
{
    public class LeituraConfiguration : IEntityTypeConfiguration<Leitura>
    {
        public void Configure(EntityTypeBuilder<Leitura> builder)
        {
            builder.ToTable("Leitura");

            // Definição da chave primária
            builder.HasKey(l => l.Id);

            // Configurações das propriedades
            builder.Property(l => l.DataLeitura)
                   .IsRequired()
                   .HasColumnType("timestamp without time zone");  // Define o tipo de coluna no PostgreSQL

            builder.Property(l => l.Valor)
                   .IsRequired();

            // Configuração de relacionamento com a entidade Tag
            builder.HasOne(l => l.Tag)
                   .WithMany(t => t.Leituras)
                   .HasForeignKey(l => l.TagId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
