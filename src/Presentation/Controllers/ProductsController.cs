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
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductsController(IProductService productService) => _productService = productService;

        [HttpGet]
        public async Task<ActionResult<List<ProductResponseDto>>> GetAll(
            [FromQuery] bool includeInactive, [FromQuery] int? categoryId, [FromQuery] string? search, CancellationToken ct) =>
            Ok(await _productService.GetAllAsync(includeInactive, categoryId, search, ct));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductResponseDto>> GetById(int id, CancellationToken ct) =>
            Ok(await _productService.GetByIdAsync(id, ct));

        [HttpPost]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<ActionResult<ProductResponseDto>> Create(CreateProductDto dto, CancellationToken ct)
        {
            var result = await _productService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<ActionResult<ProductResponseDto>> Update(int id, UpdateProductDto dto, CancellationToken ct) =>
            Ok(await _productService.UpdateAsync(id, dto, ct));

        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
        {
            await _productService.DeactivateAsync(id, ct);
            return NoContent();
        }
    }
}
