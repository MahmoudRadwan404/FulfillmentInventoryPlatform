using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface IStockAdjustmentRepository
    {
        void Add(StockAdjustment adjustment);

        Task<List<StockAdjustment>> GetRecentAsync(int? productId, int? warehouseId, int take, CancellationToken ct = default);
    }
}
