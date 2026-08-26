using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Exceptions;
using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Application.Services.Orders;
using FulfillmentInventoryPlatform.Domain.Entities;
using FulfillmentInventoryPlatform.Domain.Enums;
using Moq;
using Xunit;

namespace FulfillmentInventoryPlatform.Tests.Services
{
    public class OrderServiceTests
    {
        private readonly Mock<IOrderRepository> _orders = new();
        private readonly Mock<IOrderHistoryRepository> _history = new();
        private readonly Mock<ICustomerRepository> _customers = new();
        private readonly Mock<IProductRepository> _products = new();
        private readonly Mock<IWarehouseRepository> _warehouses = new();
        private readonly Mock<IIdempotencyService> _idempotency = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        private OrderService CreateSut()
        {
            _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
                .Returns((Func<Task> action, CancellationToken _) => action());
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _customers.Setup(c => c.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Customer { Id = 1, Name = "Acme Co", IsActive = true });

            _idempotency.Setup(i => i.TryGetAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CachedResponse?)null);

            return new OrderService(
                _orders.Object, _history.Object, _customers.Object, _products.Object,
                _warehouses.Object, _idempotency.Object, _uow.Object);
        }

        [Fact]
        public async Task CreateAsync_WithMultipleItems_SnapshotsPriceAndSumsTotal()
        {
            _products.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Product { Id = 1, Name = "Widget", Price = 10m, IsActive = true });
            _products.Setup(p => p.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Product { Id = 2, Name = "Gadget", Price = 25m, IsActive = true });
            _warehouses.Setup(w => w.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Warehouse { Id = 1, Name = "Main", IsActive = true });

            var sut = CreateSut();
            var dto = new CreateOrderDto(1, new List<CreateOrderItemDto>
            {
                new(1, 1, 3), // 3 * 10 = 30
                new(2, 1, 2)  // 2 * 25 = 50
            });

            var result = await sut.CreateAsync(dto, performedByUserId: 1, idempotencyKey: null);

            Assert.Equal(80m, result.TotalAmount);
            Assert.Equal(2, result.Items.Count);
            Assert.Contains(result.Items, i => i.ProductId == 1 && i.UnitPrice == 10m && i.LineTotal == 30m);
            Assert.Contains(result.Items, i => i.ProductId == 2 && i.UnitPrice == 25m && i.LineTotal == 50m);
            _orders.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithZeroQuantity_ThrowsValidationException()
        {
            var sut = CreateSut();
            var dto = new CreateOrderDto(1, new List<CreateOrderItemDto> { new(1, 1, 0) });

            await Assert.ThrowsAsync<ValidationException>(() => sut.CreateAsync(dto, 1, null));
            _orders.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WithNoItems_ThrowsValidationException()
        {
            var sut = CreateSut();
            var dto = new CreateOrderDto(1, new List<CreateOrderItemDto>());

            await Assert.ThrowsAsync<ValidationException>(() => sut.CreateAsync(dto, 1, null));
        }

        [Fact]
        public async Task CreateAsync_ForInactiveProduct_ThrowsValidationException()
        {
            _products.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Product { Id = 1, Name = "Widget", Price = 10m, IsActive = false });

            var sut = CreateSut();
            var dto = new CreateOrderDto(1, new List<CreateOrderItemDto> { new(1, 1, 2) });

            await Assert.ThrowsAsync<ValidationException>(() => sut.CreateAsync(dto, 1, null));
        }

        [Fact]
        public async Task AddItemAsync_WhenOrderNotPending_ThrowsConflictException()
        {
            var order = new Order { Id = 5, CustomerId = 1, Status = OrderStatus.Processing };
            _orders.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(order);

            var sut = CreateSut();

            await Assert.ThrowsAsync<ConflictException>(
                () => sut.AddItemAsync(5, new AddOrderItemDto(1, 1, 1), performedByUserId: 1));
        }

        [Fact]
        public async Task CreateAsync_WithReusedIdempotencyKey_DoesNotCreateASecondOrder()
        {
            var cachedBody = "{\"Id\":7,\"CustomerId\":1,\"CustomerName\":\"Acme Co\",\"Status\":\"Pending\"," +
                              "\"TotalAmount\":30,\"CreatedAtUtc\":\"2026-01-01T00:00:00Z\",\"UpdatedAtUtc\":null," +
                              "\"CancellationReason\":null,\"Items\":[]}";
            _idempotency.Setup(i => i.TryGetAsync("key-1", "POST:/api/orders", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CachedResponse(201, cachedBody));

            var sut = CreateSut();
            var dto = new CreateOrderDto(1, new List<CreateOrderItemDto> { new(1, 1, 3) });

            var result = await sut.CreateAsync(dto, 1, "key-1");

            Assert.Equal(7, result.Id);
            _orders.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
            _products.Verify(p => p.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
