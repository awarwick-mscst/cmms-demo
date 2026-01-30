using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class AssetPartConfiguration : IEntityTypeConfiguration<AssetPart>
{
    public void Configure(EntityTypeBuilder<AssetPart> builder)
    {
        builder.ToTable("asset_parts", "inventory");

        builder.HasKey(ap => ap.Id);
        builder.Property(ap => ap.Id).HasColumnName("id");

        builder.Property(ap => ap.AssetId)
            .HasColumnName("asset_id")
            .IsRequired();

        builder.Property(ap => ap.PartId)
            .HasColumnName("part_id")
            .IsRequired();

        builder.Property(ap => ap.QuantityUsed)
            .HasColumnName("quantity_used")
            .IsRequired();

        builder.Property(ap => ap.UnitCostAtTime)
            .HasColumnName("unit_cost_at_time")
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(ap => ap.UsedDate)
            .HasColumnName("used_date")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(ap => ap.UsedBy)
            .HasColumnName("used_by");

        builder.Property(ap => ap.WorkOrderId)
            .HasColumnName("work_order_id");

        builder.Property(ap => ap.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);

        builder.Property(ap => ap.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Ignore(ap => ap.TotalCost);

        builder.HasOne(ap => ap.Asset)
            .WithMany()
            .HasForeignKey(ap => ap.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ap => ap.Part)
            .WithMany(p => p.AssetParts)
            .HasForeignKey(ap => ap.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ap => ap.UsedByUser)
            .WithMany()
            .HasForeignKey(ap => ap.UsedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(ap => ap.AssetId);
        builder.HasIndex(ap => ap.PartId);
        builder.HasIndex(ap => ap.WorkOrderId);
        builder.HasIndex(ap => ap.UsedDate);
    }
}
