using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FulfillmentInventoryPlatform.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService) => _authService = authService;

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto dto, CancellationToken ct)
        {
            var result = await _authService.LoginAsync(dto, ct);
            return Ok(result);
        }
    }
}
