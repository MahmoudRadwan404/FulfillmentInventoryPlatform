using System.Text.Json;
using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Exceptions;
using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;
using FulfillmentInventoryPlatform.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FulfillmentInventoryPlatform.Application.Services.Orders
{
    // Owns every order-status transition and the stock side-effects that go with
    // it. Order.Status and WarehouseStock.Quantity are never written to outside
    // this service for order-related changes.
    public class OrderProcessingService : IOrderProcessingService
    {
        private const string ProcessEndpoint = "POST:/api/orders/{id}/process";
        private const string CancelEndpoint = "POST:/api/orders/{id}/cancel";
        private const int MaxConcurrencyRetries = 3;

        private readonly IOrderRepository _orders;
        private readonly IOrderHistoryRepository _history;
        private readonly ICustomerRepository _customers;
        private readonly IWarehouseStockRepository _stocks;
        private readonly IStockAdjustmentRepository _adjustments;
        private readonly IUserRepository _users;
        private readonly IIdempotencyService _idempotency;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<OrderProcessingService> _logger;

        public OrderProcessingService(
            IOrderRepository orders,
            IOrderHistoryRepository history,
            ICustomerRepository customers,
            IWarehouseStockRepository stocks,
            IStockAdjustmentRepository adjustments,
            IUserRepository users,
            IIdempotencyService idempotency,
            IUnitOfWork uow,
            ILogger<OrderProcessingService> logger)
        {
            _orders = orders;
            _history = history;
            _customers = customers;
            _stocks = stocks;
            _adjustments = adjustments;
            _users = users;
            _idempotency = idempotency;
            _uow = uow;
            _logger = logger;
        }

        public async Task<OrderResponseDto> ProcessAsync(
            int orderId, int performedByUserId, string? idempotencyKey, CancellationToken ct = default)
        {
            var cached = await _idempotency.TryGetAsync(idempotencyKey, ProcessEndpoint, ct);
            if (cached is not null)
            {
                _logger.LogInformation(
                    "Replaying cached response for order {OrderId} process request (idempotency key reused).", orderId);
                return JsonSerializer.Deserialize<OrderResponseDto>(cached.Body)!;
            }

            _ = await _users.GetByIdAsync(performedByUserId, ct) ?? throw new NotFoundException(nameof(User), performedByUserId);

            for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
            {
                try
                {
                    return await TryProcessOnceAsync(orderId, performedByUserId, idempotencyKey, ct);
                }
                catch (ConcurrencyConflictException) when (attempt < MaxConcurrencyRetries)
                {
                    _logger.LogWarning(
                        "Concurrency conflict processing order {OrderId} (attempt {Attempt}/{Max}). Retrying.",
                        orderId, attempt, MaxConcurrencyRetries);
                }
            }

            _logger.LogError(
                "Order {OrderId} could not be processed after {Max} attempts due to repeated concurrency conflicts.",
                orderId, MaxConcurrencyRetries);
            throw new ConcurrencyConflictException(
                "This order's stock could not be reserved because of concurrent updates. Please retry.");
        }

        private async Task<OrderResponseDto> TryProcessOnceAsync(
            int orderId, int performedByUserId, string? idempotencyKey, CancellationToken ct)
        {
            OrderResponseDto? result = null;

            await _uow.ExecuteInTransactionAsync(async () =>
            {
                var order = await _orders.GetByIdAsync(orderId, ct)
                    ?? throw new NotFoundException(nameof(Order), orderId);

                if (order.Status != OrderStatus.Pending)
                    throw new ConflictException(
                        $"Cannot process order in status '{order.Status}'. Only 'Pending' orders can be processed.");

                if (order.Items.Count == 0)
                    throw new ValidationException("Cannot process an order with no items.");

                // Check and deduct stock for every line as one all-or-nothing unit -
                // if any line is short, nothing is deducted (the whole transaction
                // rolls back, including the status change below).
                foreach (var item in order.Items)
                {
                    var stock = await _stocks.GetAsync(item.ProductId, item.WarehouseId, ct)
                        ?? throw new ConflictException(
                            $"Product '{item.ProductNameSnapshot}' is no longer stocked in the selected warehouse.");

                    var remaining = stock.Quantity - item.Quantity;
                    if (remaining < 0)
                        throw new ConflictException(
                            $"Insufficient stock for '{item.ProductNameSnapshot}'. Requested {item.Quantity}, available {stock.Quantity}.");

                    stock.Quantity = remaining;

                    _adjustments.Add(new StockAdjustment
                    {
                        ProductId = item.ProductId,
                        WarehouseId = item.WarehouseId,
                        Delta = -item.Quantity,
                        ResultingQuantity = remaining,
                        Reason = StockAdjustmentReason.Sale,
                        Notes = $"Consumed by Order #{order.Id}.",
                        PerformedByUserId = performedByUserId,
                        TimestampUtc = DateTime.UtcNow
                    });
                }

                var fromStatus = order.Status;
                order.Status = OrderStatus.Processing;
                order.StockDeducted = true;
                order.UpdatedAtUtc = DateTime.UtcNow;

                _history.Add(new OrderHistory
                {
                    OrderId = order.Id,
                    FromStatus = fromStatus,
                    ToStatus = order.Status,
                    ChangedByUserId = performedByUserId,
                    Notes = "Order accepted for processing; stock deducted.",
                    TimestampUtc = DateTime.UtcNow
                });

                // A RowVersion mismatch on either Order or any WarehouseStock row
                // touched above surfaces here as ConcurrencyConflictException and
                // rolls back the whole transaction - no partial stock deduction.
                await _uow.SaveChangesAsync(ct);

                var customer = await _customers.GetByIdAsync(order.CustomerId, ct);
                result = MapOrder(order, customer?.Name ?? string.Empty);

                if (!string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    _idempotency.Save(idempotencyKey, ProcessEndpoint, 200, result);
                    await _uow.SaveChangesAsync(ct);
                }

                _logger.LogInformation("Order {OrderId} processed; stock deducted for {ItemCount} item(s).", order.Id, order.Items.Count);
            }, ct);

            return result!;
        }

        public async Task<OrderResponseDto> CompleteAsync(int orderId, int performedByUserId, CancellationToken ct = default)
        {
            _ = await _users.GetByIdAsync(performedByUserId, ct) ?? throw new NotFoundException(nameof(User), performedByUserId);

            OrderResponseDto? result = null;

            await _uow.ExecuteInTransactionAsync(async () =>
            {
                var order = await _orders.GetByIdAsync(orderId, ct)
                    ?? throw new NotFoundException(nameof(Order), orderId);

                if (order.Status != OrderStatus.Processing)
                    throw new ConflictException(
                        $"Cannot complete order in status '{order.Status}'. Only orders in 'Processing' status can be completed.");

                var fromStatus = order.Status;
                order.Status = OrderStatus.Completed;
                order.UpdatedAtUtc = DateTime.UtcNow;

                _history.Add(new OrderHistory
                {
                    OrderId = order.Id,
                    FromStatus = fromStatus,
                    ToStatus = order.Status,
                    ChangedByUserId = performedByUserId,
                    Notes = "Order completed.",
                    TimestampUtc = DateTime.UtcNow
                });

                await _uow.SaveChangesAsync(ct);

                var customer = await _customers.GetByIdAsync(order.CustomerId, ct);
                result = MapOrder(order, customer?.Name ?? string.Empty);

                _logger.LogInformation("Order {OrderId} completed.", order.Id);
            }, ct);

            return result!;
        }

        public async Task<OrderResponseDto> CancelAsync(
            int orderId, CancelOrderDto dto, int performedByUserId, string? idempotencyKey, CancellationToken ct = default)
        {
            var cached = await _idempotency.TryGetAsync(idempotencyKey, CancelEndpoint, ct);
            if (cached is not null)
            {
                _logger.LogInformation(
                    "Replaying cached response for order {OrderId} cancel request (idempotency key reused).", orderId);
                return JsonSerializer.Deserialize<OrderResponseDto>(cached.Body)!;
            }

            _ = await _users.GetByIdAsync(performedByUserId, ct) ?? throw new NotFoundException(nameof(User), performedByUserId);

            for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
            {
                try
                {
                    return await TryCancelOnceAsync(orderId, dto, performedByUserId, idempotencyKey, ct);
                }
                catch (ConcurrencyConflictException) when (attempt < MaxConcurrencyRetries)
                {
                    _logger.LogWarning(
                        "Concurrency conflict cancelling order {OrderId} (attempt {Attempt}/{Max}). Retrying.",
                        orderId, attempt, MaxConcurrencyRetries);
                }
            }

            throw new ConcurrencyConflictException(
                "This order could not be cancelled because of concurrent updates. Please retry.");
        }

        private async Task<OrderResponseDto> TryCancelOnceAsync(
            int orderId, CancelOrderDto dto, int performedByUserId, string? idempotencyKey, CancellationToken ct)
        {
            OrderResponseDto? result = null;

            await _uow.ExecuteInTransactionAsync(async () =>
            {
                var order = await _orders.GetByIdAsync(orderId, ct)
                    ?? throw new NotFoundException(nameof(Order), orderId);

                if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled)
                    throw new ConflictException($"Cannot cancel an order in status '{order.Status}'.");

                // Stock is only restored if it was actually deducted (i.e. the order
                // had reached Processing). StockDeducted is flipped off immediately
                // after, so a retried/duplicate cancel attempt can never restore it twice.
                if (order.StockDeducted)
                {
                    foreach (var item in order.Items)
                    {
                        var stock = await _stocks.GetAsync(item.ProductId, item.WarehouseId, ct)
                            ?? throw new NotFoundException("Stock record missing for a cancelled order's item.");

                        stock.Quantity += item.Quantity;

                        _adjustments.Add(new StockAdjustment
                        {
                            ProductId = item.ProductId,
                            WarehouseId = item.WarehouseId,
                            Delta = item.Quantity,
                            ResultingQuantity = stock.Quantity,
                            Reason = StockAdjustmentReason.Return,
                            Notes = $"Restored by cancellation of Order #{order.Id}.",
                            PerformedByUserId = performedByUserId,
                            TimestampUtc = DateTime.UtcNow
                        });
                    }
                    order.StockDeducted = false;
                }

                var fromStatus = order.Status;
                order.Status = OrderStatus.Cancelled;
                order.CancellationReason = dto.Reason;
                order.UpdatedAtUtc = DateTime.UtcNow;

                _history.Add(new OrderHistory
                {
                    OrderId = order.Id,
                    FromStatus = fromStatus,
                    ToStatus = order.Status,
                    ChangedByUserId = performedByUserId,
                    Notes = dto.Reason is null ? "Order cancelled." : $"Order cancelled: {dto.Reason}",
                    TimestampUtc = DateTime.UtcNow
                });

                await _uow.SaveChangesAsync(ct);

                var customer = await _customers.GetByIdAsync(order.CustomerId, ct);
                result = MapOrder(order, customer?.Name ?? string.Empty);

                if (!string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    _idempotency.Save(idempotencyKey, CancelEndpoint, 200, result);
                    await _uow.SaveChangesAsync(ct);
                }

                _logger.LogInformation("Order {OrderId} cancelled (from {FromStatus}).", order.Id, fromStatus);
            }, ct);

            return result!;
        }

        private static OrderResponseDto MapOrder(Order o, string customerName) => new(
            o.Id,
            o.CustomerId,
            customerName,
            o.Status.ToString(),
            o.TotalAmount,
            o.CreatedAtUtc,
            o.UpdatedAtUtc,
            o.CancellationReason,
            o.Items.Select(i => new OrderItemResponseDto(
                i.Id, i.ProductId, i.ProductNameSnapshot, i.WarehouseId,
                i.Warehouse?.Name ?? string.Empty, i.UnitPriceSnapshot, i.Quantity, i.LineTotal)).ToList());
    }
}
