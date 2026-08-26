using System.Text.Json;
using FulfillmentInventoryPlatform.Application.Common;
using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Exceptions;
using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;
using FulfillmentInventoryPlatform.Domain.Enums;

namespace FulfillmentInventoryPlatform.Application.Services.Orders
{
    public class OrderService : IOrderService
    {
        // Endpoint identifiers used as the second half of the idempotency key.
        private const string CreateEndpoint = "POST:/api/orders";

        private readonly IOrderRepository _orders;
        private readonly IOrderHistoryRepository _history;
        private readonly ICustomerRepository _customers;
        private readonly IProductRepository _products;
        private readonly IWarehouseRepository _warehouses;
        private readonly IIdempotencyService _idempotency;
        private readonly IUnitOfWork _uow;

        public OrderService(
            IOrderRepository orders,
            IOrderHistoryRepository history,
            ICustomerRepository customers,
            IProductRepository products,
            IWarehouseRepository warehouses,
            IIdempotencyService idempotency,
            IUnitOfWork uow)
        {
            _orders = orders;
            _history = history;
            _customers = customers;
            _products = products;
            _warehouses = warehouses;
            _idempotency = idempotency;
            _uow = uow;
        }

        public async Task<OrderResponseDto> CreateAsync(
            CreateOrderDto dto, int performedByUserId, string? idempotencyKey, CancellationToken ct = default)
        {
            var cached = await _idempotency.TryGetAsync(idempotencyKey, CreateEndpoint, ct);
            if (cached is not null)
                return JsonSerializer.Deserialize<OrderResponseDto>(cached.Body)!;

            if (dto.Items is null || dto.Items.Count == 0)
                throw new ValidationException("An order must contain at least one item.");

            var customer = await _customers.GetByIdAsync(dto.CustomerId, ct)
                ?? throw new NotFoundException(nameof(Customer), dto.CustomerId);
            if (!customer.IsActive)
                throw new ValidationException("Cannot create an order for an inactive customer.");

            OrderResponseDto? result = null;

            await _uow.ExecuteInTransactionAsync(async () =>
            {
                var order = new Order
                {
                    CustomerId = dto.CustomerId,
                    Status = OrderStatus.Pending,
                    CreatedAtUtc = DateTime.UtcNow
                };

                decimal total = 0;
                foreach (var line in dto.Items)
                {
                    var (item, lineTotal) = await BuildOrderItemAsync(line, ct);
                    order.Items.Add(item);
                    total += lineTotal;
                }
                order.TotalAmount = total;

                _orders.Add(order);
                await _uow.SaveChangesAsync(ct);

                _history.Add(new OrderHistory
                {
                    OrderId = order.Id,
                    FromStatus = null,
                    ToStatus = OrderStatus.Pending,
                    ChangedByUserId = performedByUserId,
                    Notes = "Order created.",
                    TimestampUtc = DateTime.UtcNow
                });
                await _uow.SaveChangesAsync(ct);

                result = MapOrder(order, customer.Name);

                if (!string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    _idempotency.Save(idempotencyKey, CreateEndpoint, StatusCodes.Created, result);
                    await _uow.SaveChangesAsync(ct);
                }
            }, ct);

            return result!;
        }

        public async Task<OrderResponseDto> AddItemAsync(
            int orderId, AddOrderItemDto dto, int performedByUserId, CancellationToken ct = default)
        {
            OrderResponseDto? result = null;

            await _uow.ExecuteInTransactionAsync(async () =>
            {
                var order = await _orders.GetByIdAsync(orderId, ct)
                    ?? throw new NotFoundException(nameof(Order), orderId);

                // Historical order values must remain stable once processing starts -
                // items can only be added while the order is still Pending.
                if (order.Status != OrderStatus.Pending)
                    throw new ConflictException(
                        $"Cannot add items to an order in status '{order.Status}'. Only 'Pending' orders can be modified.");

                var (item, lineTotal) = await BuildOrderItemAsync(
                    new CreateOrderItemDto(dto.ProductId, dto.WarehouseId, dto.Quantity), ct);

                order.Items.Add(item);
                order.TotalAmount += lineTotal;
                order.UpdatedAtUtc = DateTime.UtcNow;

                await _uow.SaveChangesAsync(ct);

                var customer = await _customers.GetByIdAsync(order.CustomerId, ct);
                result = MapOrder(order, customer?.Name ?? string.Empty);
            }, ct);

            return result!;
        }

        public async Task<OrderResponseDto> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var order = await _orders.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Order), id);
            var customer = await _customers.GetByIdAsync(order.CustomerId, ct);
            return MapOrder(order, customer?.Name ?? string.Empty);
        }

        public async Task<PagedResult<OrderListItemDto>> GetPagedAsync(OrderQueryDto query, CancellationToken ct = default)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

            var (orders, total) = await _orders.GetPagedAsync(
                page, pageSize, query.Status, query.CustomerId, query.Search, query.SortBy, query.SortDescending, ct);

            var items = orders.Select(o => new OrderListItemDto(
                o.Id, o.CustomerId, o.Customer?.Name ?? string.Empty, o.Status.ToString(), o.TotalAmount, o.CreatedAtUtc)).ToList();

            return new PagedResult<OrderListItemDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
        }

        public async Task<List<OrderHistoryResponseDto>> GetHistoryAsync(int orderId, int take, CancellationToken ct = default)
        {
            _ = await _orders.GetByIdAsync(orderId, ct) ?? throw new NotFoundException(nameof(Order), orderId);
            var entries = await _history.GetByOrderAsync(orderId, take, ct);
            return entries.Select(h => new OrderHistoryResponseDto(
                h.Id,
                h.FromStatus?.ToString(),
                h.ToStatus.ToString(),
                h.ChangedByUser.Username,
                h.Notes,
                h.TimestampUtc)).ToList();
        }

        // Validates a line, snapshots the product's current commercial values, and
        // returns the built entity plus its line total (not yet added to any order).
        private async Task<(OrderItem Item, decimal LineTotal)> BuildOrderItemAsync(
            CreateOrderItemDto line, CancellationToken ct)
        {
            if (line.Quantity <= 0)
                throw new ValidationException("Order item quantity must be greater than zero.");

            var product = await _products.GetByIdAsync(line.ProductId, ct)
                ?? throw new NotFoundException(nameof(Product), line.ProductId);
            if (!product.IsActive)
                throw new ValidationException($"Product '{product.Name}' is not active and cannot be ordered.");

            var warehouse = await _warehouses.GetByIdAsync(line.WarehouseId, ct)
                ?? throw new NotFoundException(nameof(Warehouse), line.WarehouseId);
            if (!warehouse.IsActive)
                throw new ValidationException($"Warehouse '{warehouse.Name}' is not active.");

            // Commercial-value snapshot: taken now, from the product's current price,
            // and never rewritten later even if Product.Price subsequently changes.
            var lineTotal = product.Price * line.Quantity;

            var item = new OrderItem
            {
                ProductId = product.Id,
                ProductNameSnapshot = product.Name,
                WarehouseId = warehouse.Id,
                UnitPriceSnapshot = product.Price,
                Quantity = line.Quantity,
                LineTotal = lineTotal
            };

            return (item, lineTotal);
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

        // Local constant so this file doesn't need a reference to ASP.NET Core's
        // Microsoft.AspNetCore.Http.StatusCodes from the Application layer.
        private static class StatusCodes
        {
            public const int Created = 201;
        }
    }
}
