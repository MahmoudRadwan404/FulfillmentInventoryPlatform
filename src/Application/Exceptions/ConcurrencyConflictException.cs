namespace FulfillmentInventoryPlatform.Application.Exceptions
{
    // Thrown when an optimistic concurrency check (RowVersion) fails because
    // another request modified the same WarehouseStock row first.
    public class ConcurrencyConflictException : Exception
    {
        public ConcurrencyConflictException(string message) : base(message) { }
    }
}
