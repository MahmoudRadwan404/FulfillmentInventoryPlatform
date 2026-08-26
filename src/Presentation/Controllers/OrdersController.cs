using FulfillmentInventoryPlatform.Application.Common;
using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FulfillmentInventoryPlatform.Presentation.Controllers
{
    // Role mapping assumption (documented in README): the M1 role set (Administrator,
    // WarehouseOperator, Manager) is reused rather than adding new roles.
    //   Administrator      - sales-agent responsibilities: create/manage orders, cancel.
    //   WarehouseOperator  - stock-related processing: process and complete orders.
    //   Manager            - read-only review: browsing, details, and audit history.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private const string IdempotencyHeader = "Idempotency-Key";

        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _processingService;
        private readonly ICurrentUserService _currentUser;

        public OrdersController(
            IOrderService orderService, IOrderProcessingService processingService, ICurrentUserService currentUser)
        {
            _orderService = orderService;
            _processingService = processingService;
            _currentUser = currentUser;
        }

        // Creates an order with one or more items and calculates its total.
        // Supply an Idempotency-Key header to make a retried request safe.
        [HttpPost]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<ActionResult<OrderResponseDto>> Create(CreateOrderDto dto, CancellationToken ct)
        {
            var result = await _orderService.CreateAsync(dto, _currentUser.UserId, GetIdempotencyKey(), ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // Adds a line item to a still-Pending order and recalculates its total.
        [HttpPost("{id:int}/items")]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<ActionResult<OrderResponseDto>> AddItem(int id, AddOrderItemDto dto, CancellationToken ct) =>
            Ok(await _orderService.AddItemAsync(id, dto, _currentUser.UserId, ct));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrderResponseDto>> GetById(int id, CancellationToken ct) =>
            Ok(await _orderService.GetByIdAsync(id, ct));

        // Paged/filtered/sorted browsing - never loads the whole orders table.
        [HttpGet]
        public async Task<ActionResult<PagedResult<OrderListItemDto>>> GetPaged(
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] OrderStatus? status,
            [FromQuery] int? customerId,
            [FromQuery] string? search,
            [FromQuery] string? sortBy,
            [FromQuery] bool sortDescending,
            CancellationToken ct)
        {
            var query = new OrderQueryDto(
                page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize, status, customerId, search, sortBy, sortDescending);
            return Ok(await _orderService.GetPagedAsync(query, ct));
        }

        // Audit trail: every status transition this order has been through.
        [HttpGet("{id:int}/history")]
        public async Task<ActionResult<List<OrderHistoryResponseDto>>> GetHistory(
            int id, [FromQuery] int take, CancellationToken ct) =>
            Ok(await _orderService.GetHistoryAsync(id, take <= 0 ? 50 : take, ct));

        // Pending -> Processing. Checks and deducts stock for every line atomically.
        // Supply an Idempotency-Key header to make a retried request safe.
        [HttpPost("{id:int}/process")]
        [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.WarehouseOperator}")]
        public async Task<ActionResult<OrderResponseDto>> Process(int id, CancellationToken ct) =>
            Ok(await _processingService.ProcessAsync(id, _currentUser.UserId, GetIdempotencyKey(), ct));

        // Processing -> Completed.
        [HttpPost("{id:int}/complete")]
        [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.WarehouseOperator}")]
        public async Task<ActionResult<OrderResponseDto>> Complete(int id, CancellationToken ct) =>
            Ok(await _processingService.CompleteAsync(id, _currentUser.UserId, ct));

        // Pending/Processing -> Cancelled. Restores stock exactly once if it had
        // been deducted. Supply an Idempotency-Key header to make a retry safe.
        [HttpPost("{id:int}/cancel")]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<ActionResult<OrderResponseDto>> Cancel(int id, CancelOrderDto dto, CancellationToken ct) =>
            Ok(await _processingService.CancelAsync(id, dto, _currentUser.UserId, GetIdempotencyKey(), ct));

        private string? GetIdempotencyKey() =>
            Request.Headers.TryGetValue(IdempotencyHeader, out var value) ? value.ToString() : null;
    }
}
