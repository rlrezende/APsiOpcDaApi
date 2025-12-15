using APsiControleApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace APsiControleApi.Infrastructure.Configurations
{
    public class DetectModelTagConfiguration : IEntityTypeConfiguration<DetectModelTag>
    {
        public void Configure(EntityTypeBuilder<DetectModelTag> builder)
        {
            builder.ToTable("DetectModelTag");

            builder.HasKey(tag => tag.Id);

            builder.Property(tag => tag.TagName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(tag => tag.SeverityBaseline)
                .HasColumnType("double precision");

            builder.Property(tag => tag.ExpectedStdDev)
                .HasColumnType("double precision");

            builder.Property(tag => tag.PvMvRelation)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("none");

            builder.Property(tag => tag.Notes)
                .HasMaxLength(500);

            builder.Property(tag => tag.CreatedDate)
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        }
    }
}
