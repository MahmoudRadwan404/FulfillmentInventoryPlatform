namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public record CachedResponse(int StatusCode, string Body);

    // Thin wrapper around IIdempotencyRepository used by services that perform
    // a critical, non-repeatable business action (e.g. deducting stock).
    public interface IIdempotencyService
    {
        Task<CachedResponse?> TryGetAsync(string? key, string endpoint, CancellationToken ct = default);

        // Queues the record for insert - caller is responsible for calling
        // IUnitOfWork.SaveChangesAsync inside the same transaction as the business
        // action, so the cached response and the business change commit atomically.
        void Save(string key, string endpoint, int statusCode, object responseBody);
    }
}
