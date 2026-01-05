namespace APsiOpcDaApi.Infrastructure.Configurations
{
    using APsiOpcDaApi.Domain.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            // Nome da tabela
            builder.ToTable("Tag");

            // Chave primária
            builder.HasKey(t => t.Id);

            // Propriedades básicas
            builder.Property(t => t.Nome)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(t => t.Descricao)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(t => t.idOld)
                   .IsRequired();

            builder.Property(t => t.UnidadeId)
                   .IsRequired();

            builder.HasIndex(t => t.UnidadeId)
                   .HasDatabaseName("IX_Tag_UnidadeId");

            builder.Property(t => t.Monitora)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(t => t.ValorAtual)
                   .HasColumnType("double precision");

            builder.Property(t => t.NodeIdOpc)
                   .HasMaxLength(255)
                   .HasColumnName("NodeIdOpc");

            builder.Property(t => t.Origem)
                   .IsRequired()
                   .HasMaxLength(50)
                   .HasDefaultValue("OPCUA");

            builder.Property(t => t.NomeTabela)
                   .HasMaxLength(100);

            builder.Property(t => t.NomeColuna)
                   .HasMaxLength(100);

            // Relacionamento com OpcNode
            builder.HasOne(t => t.Node)
                   .WithMany()
                   .HasForeignKey(t => t.NodeId)
                   .OnDelete(DeleteBehavior.SetNull);

            // Relacionamento com OpcGroup
            builder.HasOne(t => t.Group)
                   .WithMany(g => g.Tags)
                   .HasForeignKey(t => t.GroupId)
                   .OnDelete(DeleteBehavior.SetNull);

            // Relacionamento com Leituras
            builder.HasMany(t => t.Leituras)
                   .WithOne(l => l.Tag)
                   .HasForeignKey(l => l.TagId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

