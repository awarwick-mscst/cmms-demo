using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class PartStockConfiguration : IEntityTypeConfiguration<PartStock>
{
    public void Configure(EntityTypeBuilder<PartStock> builder)
    {
        builder.ToTable("part_stock", "inventory");

        builder.HasKey(ps => ps.Id);
        builder.Property(ps => ps.Id).HasColumnName("id");

        builder.Property(ps => ps.PartId)
            .HasColumnName("part_id")
            .IsRequired();

        builder.Property(ps => ps.LocationId)
            .HasColumnName("location_id")
            .IsRequired();

        builder.Property(ps => ps.QuantityOnHand)
            .HasColumnName("quantity_on_hand")
            .HasDefaultValue(0);

        builder.Property(ps => ps.QuantityReserved)
            .HasColumnName("quantity_reserved")
            .HasDefaultValue(0);

        builder.Property(ps => ps.LastCountDate)
            .HasColumnName("last_count_date");

        builder.Property(ps => ps.LastCountBy)
            .HasColumnName("last_count_by");

        builder.Property(ps => ps.BinNumber)
            .HasColumnName("bin_number")
            .HasMaxLength(50);

        builder.Property(ps => ps.ShelfLocation)
            .HasColumnName("shelf_location")
            .HasMaxLength(100);

        builder.Property(ps => ps.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(ps => ps.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(ps => ps.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(ps => ps.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(ps => ps.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(ps => ps.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Ignore(ps => ps.QuantityAvailable);

        builder.HasOne(ps => ps.Part)
            .WithMany(p => p.Stocks)
            .HasForeignKey(ps => ps.PartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.Location)
            .WithMany(l => l.PartStocks)
            .HasForeignKey(ps => ps.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ps => ps.LastCountByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastCountBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(ps => new { ps.PartId, ps.LocationId })
            .IsUnique()
            .HasFilter("[is_deleted] = 0");

        builder.HasIndex(ps => ps.LocationId);

        builder.HasQueryFilter(ps => !ps.IsDeleted);
    }
}
