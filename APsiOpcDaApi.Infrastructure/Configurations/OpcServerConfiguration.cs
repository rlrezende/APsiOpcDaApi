using APsiOpcDaApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace APsiOpcDaApi.Infrastructure.Configurations
{
    public class OpcServerConfiguration : IEntityTypeConfiguration<OpcServer>
    {
        public void Configure(EntityTypeBuilder<OpcServer> builder)
        {
            builder.ToTable("OpcServer");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Nome)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(s => s.Endpoint)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(s => s.UnidadeId)
                   .IsRequired();

            builder.Property(s => s.Descricao)
                   .HasMaxLength(500);

            builder.Property(s => s.SecurityPolicy)
                   .HasMaxLength(150);

            builder.Property(s => s.SecurityMode)
                   .HasMaxLength(50);

            builder.Property(s => s.Username)
                   .HasMaxLength(100);

            builder.Property(s => s.Password)
                   .HasMaxLength(255);

            builder.Property(s => s.Host)
                   .HasMaxLength(255);

            builder.Property(s => s.ProgId)
                   .HasMaxLength(255);

            builder.Property(s => s.ClsId)
                   .HasMaxLength(255);

            builder.Property(s => s.Tipo)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(s => s.Provider)
                   .HasMaxLength(255);

            builder.Property(s => s.ConnectionString)
                   .HasMaxLength(1000);

            builder.Property(s => s.IsConnected)
                   .HasDefaultValue(false);

            builder.Property(s => s.IsOnline)
                   .HasDefaultValue(false);

            builder.Property(s => s.ResponseTime)
                   .HasDefaultValue(0);

            builder.HasMany(s => s.Nodes)
                   .WithOne(n => n.Server)
                   .HasForeignKey(n => n.ServerId)
                   .OnDelete(DeleteBehavior.Cascade);


        }
    }
}

