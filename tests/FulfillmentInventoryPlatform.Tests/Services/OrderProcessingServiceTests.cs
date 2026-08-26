using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Exceptions;
using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Application.Services.Orders;
using FulfillmentInventoryPlatform.Domain.Entities;
using FulfillmentInventoryPlatform.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FulfillmentInventoryPlatform.Tests.Services
{
    public class OrderProcessingServiceTests
    {
        private readonly Mock<IOrderRepository> _orders = new();
        private readonly Mock<IOrderHistoryRepository> _history = new();
        private readonly Mock<ICustomerRepository> _customers = new();
        private readonly Mock<IWarehouseStockRepository> _stocks = new();
        private readonly Mock<IStockAdjustmentRepository> _adjustments = new();
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<IIdempotencyService> _idempotency = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        private OrderProcessingService CreateSut()
        {
            // ExecuteInTransactionAsync just runs the delegate - no real DB/transaction
            // involved at this (Application-layer unit test) level.
            _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
                .Returns((Func<Task> action, CancellationToken _) => action());
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _users.Setup(u => u.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { Id = 1, Username = "operator" });

            _customers.Setup(c => c.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Customer { Id = 1, Name = "Acme Co" });

            _idempotency.Setup(i => i.TryGetAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CachedResponse?)null);

            return new OrderProcessingService(
                _orders.Object, _history.Object, _customers.Object, _stocks.Object,
                _adjustments.Object, _users.Object, _idempotency.Object, _uow.Object,
                Mock.Of<ILogger<OrderProcessingService>>());
        }

        private static Order PendingOrderWithOneItem(int quantity = 5) => new()
        {
            Id = 10,
            CustomerId = 1,
            Status = OrderStatus.Pending,
            Items = new List<OrderItem>
            {
                new()
                {
                    Id = 100, ProductId = 1, ProductNameSnapshot = "Widget",
                    WarehouseId = 1, Warehouse = new Warehouse { Id = 1, Name = "Main" },
                    UnitPriceSnapshot = 9.99m, Quantity = quantity, LineTotal = 9.99m * quantity
                }
            }
        };

        [Fact]
        public async Task ProcessAsync_WithSufficientStock_DeductsStockAndMovesToProcessing()
        {
            var order = PendingOrderWithOneItem(quantity: 5);
            var stock = new WarehouseStock { ProductId = 1, WarehouseId = 1, Quantity = 20 };

            _orders.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(order);
            _stocks.Setup(s => s.GetAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(stock);

            var sut = CreateSut();

            var result = await sut.ProcessAsync(10, performedByUserId: 1, idempotencyKey: null);

            Assert.Equal("Processing", result.Status);
            Assert.Equal(15, stock.Quantity); // 20 - 5
            _adjustments.Verify(a => a.Add(It.Is<StockAdjustment>(x => x.Delta == -5)), Times.Once);
            _history.Verify(h => h.Add(It.Is<OrderHistory>(x => x.ToStatus == OrderStatus.Processing)), Times.Once);
        }

        [Fact]
        public async Task ProcessAsync_WithInsufficientStock_ThrowsConflictAndDoesNotDeduct()
        {
            var order = PendingOrderWithOneItem(quantity: 50);
            var stock = new WarehouseStock { ProductId = 1, WarehouseId = 1, Quantity = 20 };

            _orders.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(order);
            _stocks.Setup(s => s.GetAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(stock);

            var sut = CreateSut();

            await Assert.ThrowsAsync<ConflictException>(() => sut.ProcessAsync(10, 1, null));

            Assert.Equal(20, stock.Quantity); // untouched
            Assert.Equal(OrderStatus.Pending, order.Status); // untouched
            _adjustments.Verify(a => a.Add(It.IsAny<StockAdjustment>()), Times.Never);
        }

        [Fact]
        public async Task ProcessAsync_WhenOrderNotPending_ThrowsConflict()
        {
            var order = PendingOrderWithOneItem();
            order.Status = OrderStatus.Completed;
            _orders.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(order);

            var sut = CreateSut();

            await Assert.ThrowsAsync<ConflictException>(() => sut.ProcessAsync(10, 1, null));
        }

        [Fact]
        public async Task ProcessAsync_WithReusedIdempotencyKey_ReturnsCachedResponseWithoutTouchingStock()
        {
            var cachedBody = "{\"Id\":10,\"CustomerId\":1,\"CustomerName\":\"Acme Co\",\"Status\":\"Processing\"," +
                              "\"TotalAmount\":49.95,\"CreatedAtUtc\":\"2026-01-01T00:00:00Z\",\"UpdatedAtUtc\":null," +
                              "\"CancellationReason\":null,\"Items\":[]}";
            _idempotency.Setup(i => i.TryGetAsync("key-1", "POST:/api/orders/{id}/process", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CachedResponse(200, cachedBody));

            var sut = CreateSut();

            var result = await sut.ProcessAsync(10, 1, "key-1");

            Assert.Equal("Processing", result.Status);
            _orders.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _stocks.Verify(s => s.GetAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompleteAsync_WhenNotProcessing_ThrowsConflict()
        {
            var order = PendingOrderWithOneItem(); // still Pending
            _orders.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(order);

            var sut = CreateSut();

            await Assert.ThrowsAsync<ConflictException>(() => sut.CompleteAsync(10, 1));
        }

        [Fact]
        public async Task CancelAsync_BeforeProcessing_DoesNotTouchStock()
        {
            var order = PendingOrderWithOneItem(); // Pending, StockDeducted == false
            _orders.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(order);

            var sut = CreateSut();

            var result = await sut.CancelAsync(10, new CancelOrderDto("Customer changed their mind"), 1, null);

            Assert.Equal("Cancelled", result.Status);
            _stocks.Verify(s => s.GetAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _adjustments.Verify(a => a.Add(It.IsAny<StockAdjustment>()), Times.Never);
        }

        [Fact]
        public async Task CancelAsync_AfterProcessing_RestoresStockExactlyOnce()
        {
            var order = PendingOrderWithOneItem(quantity: 5);
            order.Status = OrderStatus.Processing;
            order.StockDeducted = true;
            var stock = new WarehouseStock { ProductId = 1, WarehouseId = 1, Quantity = 15 }; // already deducted

            _orders.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(order);
            _stocks.Setup(s => s.GetAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(stock);

            var sut = CreateSut();

            var result = await sut.CancelAsync(10, new CancelOrderDto(null), 1, null);

            Assert.Equal("Cancelled", result.Status);
            Assert.Equal(20, stock.Quantity); // 15 + 5 restored
            Assert.False(order.StockDeducted); // guard flipped off - a second cancel attempt could never restore again
            _adjustments.Verify(a => a.Add(It.Is<StockAdjustment>(x => x.Delta == 5)), Times.Once);
        }

        [Fact]
        public async Task CancelAsync_WhenAlreadyCompleted_ThrowsConflictAndLeavesOrderUnchanged()
        {
            var order = PendingOrderWithOneItem();
            order.Status = OrderStatus.Completed;
            _orders.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(order);

            var sut = CreateSut();

            await Assert.ThrowsAsync<ConflictException>(() => sut.CancelAsync(10, new CancelOrderDto(null), 1, null));
            Assert.Equal(OrderStatus.Completed, order.Status);
        }
    }
}
