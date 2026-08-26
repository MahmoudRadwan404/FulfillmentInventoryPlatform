using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Configurations
{
    public class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
    {
        public void Configure(EntityTypeBuilder<StockAdjustment> builder)
        {
            builder.ToTable("StockAdjustments");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Reason).HasConversion<string>().HasMaxLength(30);
            builder.Property(a => a.Notes).HasMaxLength(500);

            // Adjustments are append-only: link to the WarehouseStock composite key.
            builder.HasOne(a => a.WarehouseStock)
                .WithMany(ws => ws.StockAdjustments)
                .HasForeignKey(a => new { a.ProductId, a.WarehouseId })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.PerformedByUser)
                .WithMany()
                .HasForeignKey(a => a.PerformedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => new { a.ProductId, a.WarehouseId });
            builder.HasIndex(a => a.TimestampUtc);
        }
    }
}
