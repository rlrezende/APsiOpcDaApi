using APsiOpcDaApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace APsiOpcDaApi.Infrastructure.Configurations
{
    public class OpcNodeConfiguration : IEntityTypeConfiguration<OpcNode>
    {
        public void Configure(EntityTypeBuilder<OpcNode> builder)
        {
            builder.ToTable("OpcNode");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.Nome)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(n => n.NodeId)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(n => n.ServerId)
                   .IsRequired();

            builder.HasIndex(n => n.ServerId)
                   .HasDatabaseName("IX_OpcNode_ServerId");

            builder.HasOne(n => n.Server)
                   .WithMany(s => s.Nodes)
                   .HasForeignKey(n => n.ServerId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

