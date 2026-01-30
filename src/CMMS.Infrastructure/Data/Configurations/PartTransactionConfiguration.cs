using CMMS.Core.Entities;
using CMMS.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class PartTransactionConfiguration : IEntityTypeConfiguration<PartTransaction>
{
    public void Configure(EntityTypeBuilder<PartTransaction> builder)
    {
        builder.ToTable("part_transactions", "inventory");

        builder.HasKey(pt => pt.Id);
        builder.Property(pt => pt.Id).HasColumnName("id");

        builder.Property(pt => pt.PartId)
            .HasColumnName("part_id")
            .IsRequired();

        builder.Property(pt => pt.LocationId)
            .HasColumnName("location_id");

        builder.Property(pt => pt.ToLocationId)
            .HasColumnName("to_location_id");

        builder.Property(pt => pt.TransactionType)
            .HasColumnName("transaction_type")
            .HasMaxLength(20)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<TransactionType>(v))
            .IsRequired();

        builder.Property(pt => pt.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(pt => pt.UnitCost)
            .HasColumnName("unit_cost")
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(pt => pt.ReferenceType)
            .HasColumnName("reference_type")
            .HasMaxLength(50);

        builder.Property(pt => pt.ReferenceId)
            .HasColumnName("reference_id");

        builder.Property(pt => pt.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);

        builder.Property(pt => pt.TransactionDate)
            .HasColumnName("transaction_date")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(pt => pt.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(pt => pt.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(pt => pt.Part)
            .WithMany(p => p.Transactions)
            .HasForeignKey(pt => pt.PartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pt => pt.Location)
            .WithMany()
            .HasForeignKey(pt => pt.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pt => pt.ToLocation)
            .WithMany()
            .HasForeignKey(pt => pt.ToLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pt => pt.CreatedByUser)
            .WithMany()
            .HasForeignKey(pt => pt.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(pt => pt.PartId);
        builder.HasIndex(pt => pt.LocationId);
        builder.HasIndex(pt => pt.TransactionDate);
        builder.HasIndex(pt => new { pt.ReferenceType, pt.ReferenceId });
    }
}
