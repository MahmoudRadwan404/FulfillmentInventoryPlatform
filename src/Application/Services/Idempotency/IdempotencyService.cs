using System.Text.Json;
using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Services.Idempotency
{
    public class IdempotencyService : IIdempotencyService
    {
        private readonly IIdempotencyRepository _repository;
        public IdempotencyService(IIdempotencyRepository repository) => _repository = repository;

        public async Task<CachedResponse?> TryGetAsync(string? key, string endpoint, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var record = await _repository.GetAsync(key, endpoint, ct);
            return record is null ? null : new CachedResponse(record.ResponseStatusCode, record.ResponseBody);
        }

        public void Save(string key, string endpoint, int statusCode, object responseBody)
        {
            _repository.Add(new IdempotencyRecord
            {
                Key = key,
                Endpoint = endpoint,
                ResponseStatusCode = statusCode,
                ResponseBody = JsonSerializer.Serialize(responseBody),
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }
}
