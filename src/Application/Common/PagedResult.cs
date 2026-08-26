namespace FulfillmentInventoryPlatform.Application.Common
{
    // Generic paged-list envelope so growing lists (orders, order history, ...)
    // never require loading the entire dataset into memory.
    public class PagedResult<T>
    {
        public List<T> Items { get; init; } = new();
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
