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
    public class WarehousesController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;
        public WarehousesController(IWarehouseService warehouseService) => _warehouseService = warehouseService;

        [HttpGet]
        public async Task<ActionResult<List<WarehouseResponseDto>>> GetAll([FromQuery] bool includeInactive, CancellationToken ct) =>
            Ok(await _warehouseService.GetAllAsync(includeInactive, ct));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<WarehouseResponseDto>> GetById(int id, CancellationToken ct) =>
            Ok(await _warehouseService.GetByIdAsync(id, ct));

        [HttpPost]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<ActionResult<WarehouseResponseDto>> Create(CreateWarehouseDto dto, CancellationToken ct)
        {
            var result = await _warehouseService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<ActionResult<WarehouseResponseDto>> Update(int id, UpdateWarehouseDto dto, CancellationToken ct) =>
            Ok(await _warehouseService.UpdateAsync(id, dto, ct));

        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
        {
            await _warehouseService.DeactivateAsync(id, ct);
            return NoContent();
        }
    }
}
