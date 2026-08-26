using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders", t =>
                t.HasCheckConstraint("CK_Orders_TotalAmount_NonNegative", "[TotalAmount] >= 0"));

            builder.HasKey(o => o.Id);

            builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(o => o.TotalAmount).HasPrecision(18, 2);
            builder.Property(o => o.CancellationReason).HasMaxLength(500);

            // Optimistic concurrency token for the order header (Status/TotalAmount),
            // separate from WarehouseStock's own RowVersion which guards Quantity.
            builder.Property(o => o.RowVersion).IsRowVersion();

            builder.HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Supports paged/filtered order browsing without a full table scan.
            builder.HasIndex(o => o.Status);
            builder.HasIndex(o => o.CreatedAtUtc);
            builder.HasIndex(o => o.CustomerId);
        }
    }
}
