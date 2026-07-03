using APsiOpcDaApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace APsiOpcDaApi.Infrastructure.Configurations
{
    public class OpcGroupConfiguration : IEntityTypeConfiguration<OpcGroup>
    {
              public void Configure(EntityTypeBuilder<OpcGroup> builder)
              {

              builder.ToTable("OpcGroup");

              builder.HasKey(g => g.Id);

              builder.Property(g => g.Name)
                     .IsRequired()
                     .HasMaxLength(100);

              builder.Property(g => g.Description)
                     .HasMaxLength(500);

              builder.Property(g => g.UpdateRate)
                     .IsRequired()
                     .HasDefaultValue(1000);

              builder.Property(g => g.KeepAliveCount)
                     .IsRequired()
                     .HasDefaultValue(10);

              builder.Property(g => g.LifetimeCount)
                     .IsRequired()
                     .HasDefaultValue(100);

              builder.Property(g => g.MaxNotificationsPerPublish)
                     .IsRequired()
                     .HasDefaultValue(1000);

              builder.Property(g => g.Priority)
                     .IsRequired()
                     .HasDefaultValue((byte)100);

              builder.Property(g => g.Deadband)
                     .IsRequired()
                     .HasDefaultValue(0.1);

              builder.Property(g => g.HistorianIntervalSeconds)
                     .IsRequired()
                     .HasDefaultValue(30);

              builder.Property(g => g.AcquisitionMode)
                     .IsRequired()
                     .HasDefaultValue(1);

              builder.Property(g => g.IsActive)
                     .IsRequired()
                     .HasDefaultValue(false);

              builder.HasOne(g => g.Server)
                     .WithMany(s => s.Groups)
                     .HasForeignKey(g => g.ServerId)
                     .OnDelete(DeleteBehavior.Cascade);

              builder.HasIndex(g => g.ServerId)
                     .HasDatabaseName("IX_OpcGroup_ServerId");
              }

    }
}

