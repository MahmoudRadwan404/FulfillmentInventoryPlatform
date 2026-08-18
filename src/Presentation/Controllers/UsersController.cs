using FulfillmentInventoryPlatform.Application.Common;
using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FulfillmentInventoryPlatform.Presentation.Controllers
{
    // Administrator-only: create/update/deactivate application users.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RoleNames.Administrator)]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        public UsersController(IUserService userService) => _userService = userService;

        [HttpGet]
        public async Task<ActionResult<List<UserResponseDto>>> GetAll(CancellationToken ct) =>
            Ok(await _userService.GetAllAsync(ct));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserResponseDto>> GetById(int id, CancellationToken ct) =>
            Ok(await _userService.GetByIdAsync(id, ct));

        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> Create(CreateUserDto dto, CancellationToken ct)
        {
            var result = await _userService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<UserResponseDto>> Update(int id, UpdateUserDto dto, CancellationToken ct) =>
            Ok(await _userService.UpdateAsync(id, dto, ct));

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
        {
            await _userService.DeactivateAsync(id, ct);
            return NoContent();
        }
    }
}
