using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItems", t =>
            {
                // Invalid quantities/values must never reach the database, even if
                // application-level validation is ever bypassed or has a bug.
                t.HasCheckConstraint("CK_OrderItems_Quantity_Positive", "[Quantity] > 0");
                t.HasCheckConstraint("CK_OrderItems_UnitPrice_NonNegative", "[UnitPriceSnapshot] >= 0");
                t.HasCheckConstraint("CK_OrderItems_LineTotal_NonNegative", "[LineTotal] >= 0");
            });

            builder.HasKey(oi => oi.Id);

            builder.Property(oi => oi.ProductNameSnapshot).IsRequired().HasMaxLength(200);
            builder.Property(oi => oi.UnitPriceSnapshot).HasPrecision(18, 2);
            builder.Property(oi => oi.LineTotal).HasPrecision(18, 2);

            // Items belong to their order (aggregate ownership) - orders are never
            // hard-deleted in this app (only moved to the Cancelled status), so this
            // cascade only ever matters for local dev/test data cleanup.
            builder.HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(oi => oi.Warehouse)
                .WithMany()
                .HasForeignKey(oi => oi.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(oi => oi.OrderId);
        }
    }
}
