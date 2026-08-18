using FulfillmentInventoryPlatform.Application.DTOs;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface IStockService
    {
        Task<WarehouseStockResponseDto> AssignProductToWarehouseAsync(AssignProductToWarehouseDto dto, int performedByUserId, CancellationToken ct = default);

        Task<WarehouseStockResponseDto> AdjustStockAsync(AdjustStockDto dto, int performedByUserId, CancellationToken ct = default);

        Task<List<WarehouseStockResponseDto>> GetStockByProductAsync(int productId, CancellationToken ct = default);

        Task<List<WarehouseStockResponseDto>> GetStockByWarehouseAsync(int warehouseId, CancellationToken ct = default);

        Task<List<StockAdjustmentResponseDto>> GetRecentAdjustmentsAsync(int? productId, int? warehouseId, int take, CancellationToken ct = default);
    }
}
