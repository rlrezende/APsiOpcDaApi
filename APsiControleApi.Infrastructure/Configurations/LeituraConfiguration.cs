namespace APsiControleApi.Infrastructure.Configurations
{
    using APsiControleApi.Domain.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class LeituraConfiguration : IEntityTypeConfiguration<Leitura>
    {
        public void Configure(EntityTypeBuilder<Leitura> builder)
        {
            builder.ToTable("Leitura");

            // Definição da chave primária
            builder.HasKey(l => l.Id);

            // Configurações das propriedades
            builder.Property(l => l.DataLeitura)
                   .IsRequired();

            builder.Property(l => l.Valor)
                   .IsRequired();

            // Configuração de relacionamento com a entidade Tag
            builder.HasOne(l => l.Tag)
                   .WithMany(t => t.Leituras) // Assumindo que a Tag tem uma coleção de Leituras
                   .HasForeignKey(l => l.TagId)
                   .OnDelete(DeleteBehavior.Cascade); // Deleção em cascata para as Leituras ao remover uma Tag
        }
    }
}
