using APsiOpcDaApi.Domain.Entities;
using APsiOpcDaApi.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace APsiOpcDaApi.Infrastructure.Configurations
{
    public class DetectModelConfiguration : IEntityTypeConfiguration<DetectModel>
    {
        public void Configure(EntityTypeBuilder<DetectModel> builder)
        {
            builder.ToTable("DetectModel");

            builder.HasKey(model => model.Id);

            builder.Property(model => model.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(model => model.Description)
                .HasMaxLength(500);

            builder.Property(model => model.InstrumentClass)
                .HasMaxLength(100);

            builder.Property(model => model.ScheduleMinutes)
                .IsRequired();

            builder.Property(model => model.TargetAccuracy)
                .HasColumnType("double precision")
                .HasDefaultValue(0);

            builder.Property(model => model.Status)
                .HasConversion<int>()
                .HasDefaultValue(DetectModelStatus.Draft);

            builder.Property(model => model.IsActive)
                .HasDefaultValue(false);

            builder.Property(model => model.DriftPercent)
                .HasColumnType("double precision")
                .HasDefaultValue(0);

            builder.Property(model => model.CreatedDate)
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

            builder.HasMany(model => model.Tags)
                .WithOne(tag => tag.DetectModel)
                .HasForeignKey(tag => tag.DetectModelId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(model => model.Pipelines)
                .WithOne(pipeline => pipeline.DetectModel)
                .HasForeignKey(pipeline => pipeline.DetectModelId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(model => model.TrainingJobs)
                .WithOne(job => job.DetectModel)
                .HasForeignKey(job => job.DetectModelId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

