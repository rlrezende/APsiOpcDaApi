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

            // Chave primária
            builder.HasKey(l => l.Id);

            // Conversor de NodaTime.Instant para DateTimeOffset
            var instantConverter = new ValueConverter<Instant, DateTimeOffset>(
                v => v.ToDateTimeOffset(),
                v => Instant.FromDateTimeOffset(v));

            builder.Property(l => l.DataLeitura)
                   .IsRequired()
                   .HasConversion(instantConverter)
                   .HasColumnType("timestamp with time zone");

            // Valor numérico principal
            builder.Property(l => l.Valor)
                   .IsRequired();

            // ValorBruto (pode armazenar strings como "123.45", "erro", etc)
            builder.Property(l => l.ValorBruto)
                   .HasMaxLength(100);

            // Erro (mensagem opcional de falha)
            builder.Property(l => l.Erro)
                   .HasMaxLength(500);

            // Relacionamento com Tag
            builder.HasOne(l => l.Tag)
                   .WithMany(t => t.Leituras)
                   .HasForeignKey(l => l.TagId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
