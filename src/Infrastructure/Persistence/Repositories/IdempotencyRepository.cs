using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Repositories
{
    public class IdempotencyRepository : IIdempotencyRepository
    {
        private readonly AppDbContext _db;
        public IdempotencyRepository(AppDbContext db) => _db = db;

        public Task<IdempotencyRecord?> GetAsync(string key, string endpoint, CancellationToken ct = default) =>
            _db.IdempotencyRecords.FirstOrDefaultAsync(r => r.Key == key && r.Endpoint == endpoint, ct);

        public void Add(IdempotencyRecord record) => _db.IdempotencyRecords.Add(record);
    }
}
