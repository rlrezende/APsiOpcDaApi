using APsiOpcDaApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace APsiOpcDaApi.Infrastructure.Configurations
{
    public class DetectModelPipelineConfiguration : IEntityTypeConfiguration<DetectModelPipeline>
    {
        public void Configure(EntityTypeBuilder<DetectModelPipeline> builder)
        {
            builder.ToTable("DetectModelPipeline");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.PipelineKey)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.CreatedDate)
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        }
    }
}

