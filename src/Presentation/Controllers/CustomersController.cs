using FulfillmentInventoryPlatform.Application.Common;
using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FulfillmentInventoryPlatform.Presentation.Controllers
{
    // Minimal - just enough for orders to belong to a customer (see README assumptions).
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        public CustomersController(ICustomerService customerService) => _customerService = customerService;

        [HttpPost]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<ActionResult<CustomerResponseDto>> Create(CreateCustomerDto dto, CancellationToken ct)
        {
            var result = await _customerService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CustomerResponseDto>> GetById(int id, CancellationToken ct) =>
            Ok(await _customerService.GetByIdAsync(id, ct));

        [HttpGet]
        public async Task<ActionResult<List<CustomerResponseDto>>> GetAll([FromQuery] bool includeInactive, CancellationToken ct) =>
            Ok(await _customerService.GetAllAsync(includeInactive, ct));
    }
}
