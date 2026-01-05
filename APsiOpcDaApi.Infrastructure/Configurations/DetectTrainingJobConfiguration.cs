using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace APsiOpcDaApi.Infrastructure.Configurations
{
    public class DetectTrainingJobConfiguration : IEntityTypeConfiguration<DetectTrainingJob>
    {
        public void Configure(EntityTypeBuilder<DetectTrainingJob> builder)
        {
            builder.ToTable("DetectTrainingJob");

            builder.HasKey(job => job.Id);

            builder.Property(job => job.Status)
                .HasConversion<int>()
                .HasDefaultValue(DetectTrainingStatus.Pending);

            builder.Property(job => job.Notes)
                .HasMaxLength(500);

            builder.Property(job => job.CreatedDate)
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        }
    }
}

