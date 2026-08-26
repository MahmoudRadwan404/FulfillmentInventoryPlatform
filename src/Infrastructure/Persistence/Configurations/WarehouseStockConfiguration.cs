using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Configurations
{
    public class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStock>
    {
        public void Configure(EntityTypeBuilder<WarehouseStock> builder)
        {
            builder.ToTable("WarehouseStocks", t =>
                // DB-level backstop for the M2 constraint "stock must never become invalid" -
                // application code already checks this, this catches anything that doesn't.
                t.HasCheckConstraint("CK_WarehouseStocks_Quantity_NonNegative", "[Quantity] >= 0"));

            // Composite key: a product has at most one stock row per warehouse.
            builder.HasKey(ws => new { ws.ProductId, ws.WarehouseId });

            builder.Property(ws => ws.Quantity).IsRequired();

            // Optimistic concurrency token - EF Core checks this on every UPDATE
            // and throws DbUpdateConcurrencyException on a stale write.
            builder.Property(ws => ws.RowVersion).IsRowVersion();

            builder.HasOne(ws => ws.Product)
                .WithMany(p => p.WarehouseStocks)
                .HasForeignKey(ws => ws.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ws => ws.Warehouse)
                .WithMany(w => w.WarehouseStocks)
                .HasForeignKey(ws => ws.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
