using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace APsiOpcDaApi.Infrastructure.Configurations
{
    public static class GlobalEntityConfiguration
    {
        // Método de extensão
        public static void ConfigureBaseEntity<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : class
        {
            // Configura a propriedade "CreatedDate"
            builder.Property<DateTime>("CreatedDate")
                   .IsRequired()
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Configura a propriedade "UpdatedDate"
            builder.Property<DateTime?>("UpdatedDate")
                   .IsRequired(false);
        }
    }
}

