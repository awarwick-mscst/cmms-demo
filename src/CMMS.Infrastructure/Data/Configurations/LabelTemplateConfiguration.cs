using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class LabelTemplateConfiguration : IEntityTypeConfiguration<LabelTemplate>
{
    public void Configure(EntityTypeBuilder<LabelTemplate> builder)
    {
        builder.ToTable("label_templates", "admin");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(t => t.Width)
            .HasColumnName("width")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(t => t.Height)
            .HasColumnName("height")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(t => t.Dpi)
            .HasColumnName("dpi")
            .HasDefaultValue(203);

        builder.Property(t => t.ElementsJson)
            .HasColumnName("elements_json")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(t => t.IsDefault)
            .HasColumnName("is_default")
            .HasDefaultValue(false);

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(t => t.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(t => t.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(t => t.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(t => t.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasIndex(t => t.Name)
            .IsUnique()
            .HasFilter("[is_deleted] = 0");

        builder.HasIndex(t => t.IsDefault)
            .HasFilter("[is_deleted] = 0");

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
