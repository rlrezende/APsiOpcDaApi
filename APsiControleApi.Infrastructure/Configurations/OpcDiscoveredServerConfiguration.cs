using APsiControleApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace APsiControleApi.Infrastructure.Configurations
{
    public class OpcDiscoveredServerConfiguration : IEntityTypeConfiguration<OpcDiscoveredServer>
    {
        public void Configure(EntityTypeBuilder<OpcDiscoveredServer> builder)
        {
            builder.ToTable("OpcDiscoveredServer");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(s => s.Endpoint)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(s => s.ApplicationUri)
                   .HasMaxLength(255);

            builder.Property(s => s.SecurityModes)
                   .HasMaxLength(500);

            builder.Property(s => s.NetworkRange)
                   .HasMaxLength(50);

            builder.HasIndex(s => s.Endpoint)
                   .IsUnique()
                   .HasDatabaseName("IX_OpcDiscoveredServer_Endpoint");
        }
    }
}
