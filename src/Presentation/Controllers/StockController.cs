using FulfillmentInventoryPlatform.Application.Common;
using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FulfillmentInventoryPlatform.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly ICurrentUserService _currentUser;

        public StockController(IStockService stockService, ICurrentUserService currentUser)
        {
            _stockService = stockService;
            _currentUser = currentUser;
        }

        // Assign a product to a warehouse with a starting quantity.
        // Administrator or Warehouse Operator.
        [HttpPost("assign")]
        [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.WarehouseOperator}")]
        public async Task<ActionResult<WarehouseStockResponseDto>> Assign(AssignProductToWarehouseDto dto, CancellationToken ct)
        {
            var result = await _stockService.AssignProductToWarehouseAsync(dto, _currentUser.UserId, ct);
            return Ok(result);
        }

        // Increase or decrease stock (Delta can be negative). Administrator or Warehouse Operator.
        [HttpPost("adjust")]
        [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.WarehouseOperator}")]
        public async Task<ActionResult<WarehouseStockResponseDto>> Adjust(AdjustStockDto dto, CancellationToken ct)
        {
            var result = await _stockService.AdjustStockAsync(dto, _currentUser.UserId, ct);
            return Ok(result);
        }

        [HttpGet("product/{productId:int}")]
        public async Task<ActionResult<List<WarehouseStockResponseDto>>> GetByProduct(int productId, CancellationToken ct) =>
            Ok(await _stockService.GetStockByProductAsync(productId, ct));

        [HttpGet("warehouse/{warehouseId:int}")]
        public async Task<ActionResult<List<WarehouseStockResponseDto>>> GetByWarehouse(int warehouseId, CancellationToken ct) =>
            Ok(await _stockService.GetStockByWarehouseAsync(warehouseId, ct));

        // Recent stock adjustment history, optionally filtered by product and/or warehouse.
        [HttpGet("history")]
        public async Task<ActionResult<List<StockAdjustmentResponseDto>>> GetHistory(
            [FromQuery] int? productId, [FromQuery] int? warehouseId, [FromQuery] int take, CancellationToken ct) =>
            Ok(await _stockService.GetRecentAdjustmentsAsync(productId, warehouseId, take <= 0 ? 50 : take, ct));
    }
}
