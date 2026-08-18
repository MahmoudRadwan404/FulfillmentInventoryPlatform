using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Exceptions;
using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;
using FulfillmentInventoryPlatform.Domain.Enums;

namespace FulfillmentInventoryPlatform.Application.Services
{
    // Owns all stock-mutation logic. WarehouseStock.Quantity must NEVER be
    // written to outside this service - every change goes through here so
    // that a matching StockAdjustment row is always created (traceability).
    public class StockService : IStockService
    {
        private readonly IWarehouseStockRepository _stocks;
        private readonly IStockAdjustmentRepository _adjustments;
        private readonly IProductRepository _products;
        private readonly IWarehouseRepository _warehouses;
        private readonly IUserRepository _users;
        private readonly IUnitOfWork _uow;

        public StockService(
            IWarehouseStockRepository stocks,
            IStockAdjustmentRepository adjustments,
            IProductRepository products,
            IWarehouseRepository warehouses,
            IUserRepository users,
            IUnitOfWork uow)
        {
            _stocks = stocks;
            _adjustments = adjustments;
            _products = products;
            _warehouses = warehouses;
            _users = users;
            _uow = uow;
        }

        public async Task<WarehouseStockResponseDto> AssignProductToWarehouseAsync(
            AssignProductToWarehouseDto dto, int performedByUserId, CancellationToken ct = default)
        {
            if (dto.InitialQuantity < 0)
                throw new ValidationException("Initial quantity cannot be negative.");

            var product = await _products.GetByIdAsync(dto.ProductId, ct)
                ?? throw new NotFoundException(nameof(Product), dto.ProductId);
            if (!product.IsActive)
                throw new ValidationException("Cannot assign stock for an inactive product.");

            var warehouse = await _warehouses.GetByIdAsync(dto.WarehouseId, ct)
                ?? throw new NotFoundException(nameof(Warehouse), dto.WarehouseId);
            if (!warehouse.IsActive)
                throw new ValidationException("Cannot assign stock to an inactive warehouse.");

            if (await _stocks.ExistsAsync(dto.ProductId, dto.WarehouseId, ct))
                throw new ConflictException("This product is already assigned to this warehouse. Use a stock adjustment instead.");

            WarehouseStock? created = null;

            await _uow.ExecuteInTransactionAsync(async () =>
            {
                var stock = new WarehouseStock
                {
                    ProductId = dto.ProductId,
                    WarehouseId = dto.WarehouseId,
                    Quantity = 0
                };
                _stocks.Add(stock);
                await _uow.SaveChangesAsync(ct);

                if (dto.InitialQuantity > 0)
                {
                    stock.Quantity = dto.InitialQuantity;
                    _adjustments.Add(new StockAdjustment
                    {
                        ProductId = dto.ProductId,
                        WarehouseId = dto.WarehouseId,
                        Delta = dto.InitialQuantity,
                        ResultingQuantity = dto.InitialQuantity,
                        Reason = StockAdjustmentReason.InitialStock,
                        Notes = "Initial stock on assignment to warehouse.",
                        PerformedByUserId = performedByUserId,
                        TimestampUtc = DateTime.UtcNow
                    });
                    await _uow.SaveChangesAsync(ct);
                }

                created = stock;
            }, ct);

            return await MapStockAsync(created!, ct);
        }

        public async Task<WarehouseStockResponseDto> AdjustStockAsync(
            AdjustStockDto dto, int performedByUserId, CancellationToken ct = default)
        {
            if (dto.Delta == 0)
                throw new ValidationException("Adjustment delta cannot be zero.");

            var user = await _users.GetByIdAsync(performedByUserId, ct)
                ?? throw new NotFoundException(nameof(User), performedByUserId);

            WarehouseStock? updated = null;

            try
            {
                await _uow.ExecuteInTransactionAsync(async () =>
                {
                    var stock = await _stocks.GetAsync(dto.ProductId, dto.WarehouseId, ct)
                        ?? throw new NotFoundException("This product is not currently assigned to that warehouse.");

                    var newQuantity = stock.Quantity + dto.Delta;
                    if (newQuantity < 0)
                        throw new ValidationException(
                            $"Adjustment would result in negative stock ({newQuantity}). Current quantity is {stock.Quantity}.");

                    stock.Quantity = newQuantity;

                    _adjustments.Add(new StockAdjustment
                    {
                        ProductId = dto.ProductId,
                        WarehouseId = dto.WarehouseId,
                        Delta = dto.Delta,
                        ResultingQuantity = newQuantity,
                        Reason = dto.Reason,
                        Notes = dto.Notes,
                        PerformedByUserId = performedByUserId,
                        TimestampUtc = DateTime.UtcNow
                    });

                    // SaveChangesAsync will throw ConcurrencyConflictException (wrapped by the
                    // Infrastructure UnitOfWork) if the RowVersion no longer matches - meaning
                    // someone else adjusted this same stock row first.
                    await _uow.SaveChangesAsync(ct);

                    updated = stock;
                }, ct);
            }
            catch (ConcurrencyConflictException)
            {
                throw new ConcurrencyConflictException(
                    "This stock record was modified by someone else at the same time. Please retry the adjustment.");
            }

            return await MapStockAsync(updated!, ct);
        }

        public async Task<List<WarehouseStockResponseDto>> GetStockByProductAsync(int productId, CancellationToken ct = default)
        {
            _ = await _products.GetByIdAsync(productId, ct) ?? throw new NotFoundException(nameof(Product), productId);
            var stocks = await _stocks.GetByProductAsync(productId, ct);
            return stocks.Select(MapStock).ToList();
        }

        public async Task<List<WarehouseStockResponseDto>> GetStockByWarehouseAsync(int warehouseId, CancellationToken ct = default)
        {
            _ = await _warehouses.GetByIdAsync(warehouseId, ct) ?? throw new NotFoundException(nameof(Warehouse), warehouseId);
            var stocks = await _stocks.GetByWarehouseAsync(warehouseId, ct);
            return stocks.Select(MapStock).ToList();
        }

        public async Task<List<StockAdjustmentResponseDto>> GetRecentAdjustmentsAsync(
            int? productId, int? warehouseId, int take, CancellationToken ct = default)
        {
            var adjustments = await _adjustments.GetRecentAsync(productId, warehouseId, take, ct);
            return adjustments.Select(a => new StockAdjustmentResponseDto(
                a.Id,
                a.ProductId,
                a.WarehouseStock.Product.Name,
                a.WarehouseId,
                a.WarehouseStock.Warehouse.Name,
                a.Delta,
                a.ResultingQuantity,
                a.Reason.ToString(),
                a.Notes,
                a.PerformedByUser.Username,
                a.TimestampUtc)).ToList();
        }

        private static WarehouseStockResponseDto MapStock(WarehouseStock s) => new(
            s.ProductId, s.Product?.Name ?? string.Empty,
            s.WarehouseId, s.Warehouse?.Name ?? string.Empty,
            s.Quantity);

        private async Task<WarehouseStockResponseDto> MapStockAsync(WarehouseStock s, CancellationToken ct)
        {
            // Re-fetch with navigation properties loaded for a complete response DTO.
            var full = await _stocks.GetAsync(s.ProductId, s.WarehouseId, ct);
            return MapStock(full!);
        }
    }
}
