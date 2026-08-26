using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface IIdempotencyRepository
    {
        Task<IdempotencyRecord?> GetAsync(string key, string endpoint, CancellationToken ct = default);
        void Add(IdempotencyRecord record);
    }
}
