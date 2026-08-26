using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Configurations
{
    public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
    {
        public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.ToTable("IdempotencyRecords");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Key).IsRequired().HasMaxLength(200);
            builder.Property(r => r.Endpoint).IsRequired().HasMaxLength(200);
            builder.Property(r => r.ResponseBody).IsRequired();

            // The actual safety net: even if two identical requests race each other
            // and both pass the application-level "already exists?" check, only one
            // insert can succeed here - the other fails with a unique-constraint
            // violation, which the caller treats as "someone else already handled this".
            builder.HasIndex(r => new { r.Key, r.Endpoint }).IsUnique();
        }
    }
}
