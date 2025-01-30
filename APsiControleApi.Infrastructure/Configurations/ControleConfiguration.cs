namespace APsiControleApi.Infrastructure.Configurations
{
    using APsiControleApi.Domain.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class ControleConfiguration : IEntityTypeConfiguration<Controle>
    {
        public void Configure(EntityTypeBuilder<Controle> builder)
        {
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

            // Configuração de relacionamento com a entidade Unidade
            builder.HasOne(c => c.Unidade)
                   .WithMany(u => u.Controles) // Assumindo que a Unidade tem uma coleção de Controles
                   .HasForeignKey(c => c.UnidadeId)
                   .OnDelete(DeleteBehavior.Restrict); // Evita exclusão em cascata
        }
    }
}