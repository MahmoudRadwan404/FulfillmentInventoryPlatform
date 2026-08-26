using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Configurations
{
    public class OrderHistoryConfiguration : IEntityTypeConfiguration<OrderHistory>
    {
        public void Configure(EntityTypeBuilder<OrderHistory> builder)
        {
            builder.ToTable("OrderHistories");
            builder.HasKey(h => h.Id);

            builder.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(20);
            builder.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(h => h.Notes).HasMaxLength(500);

            builder.HasOne(h => h.Order)
                .WithMany(o => o.History)
                .HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(h => h.ChangedByUser)
                .WithMany()
                .HasForeignKey(h => h.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(h => new { h.OrderId, h.TimestampUtc });
        }
    }
}
