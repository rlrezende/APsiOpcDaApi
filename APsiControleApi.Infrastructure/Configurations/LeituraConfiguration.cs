using APsiControleApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
            var instantConverter = new ValueConverter<Instant, DateTimeOffset>(
                v => v.ToDateTimeOffset(), // Convert from Instant to DateTimeOffset
                v => Instant.FromDateTimeOffset(v)); // Convert from DateTimeOffset to Instant

            builder.Property(l => l.DataLeitura)
                   .IsRequired()
                   .HasConversion(instantConverter)
                   .HasColumnType("timestamp with time zone");  // Mudança para timestamp with time zone

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
