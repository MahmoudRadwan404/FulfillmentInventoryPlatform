using FulfillmentInventoryPlatform.Application.Common;
using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FulfillmentInventoryPlatform.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // any authenticated role may read
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        public CategoriesController(ICategoryService categoryService) => _categoryService = categoryService;

        [HttpGet]
        public async Task<ActionResult<List<CategoryResponseDto>>> GetAll([FromQuery] bool includeInactive, CancellationToken ct) =>
            Ok(await _categoryService.GetAllAsync(includeInactive, ct));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoryResponseDto>> GetById(int id, CancellationToken ct) =>
            Ok(await _categoryService.GetByIdAsync(id, ct));

        [HttpPost]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<ActionResult<CategoryResponseDto>> Create(CreateCategoryDto dto, CancellationToken ct)
        {
            var result = await _categoryService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<ActionResult<CategoryResponseDto>> Update(int id, UpdateCategoryDto dto, CancellationToken ct) =>
            Ok(await _categoryService.UpdateAsync(id, dto, ct));

        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
        {
            await _categoryService.DeactivateAsync(id, ct);
            return NoContent();
        }
    }
}
