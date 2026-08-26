using FulfillmentInventoryPlatform.Application.DTOs;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<CustomerResponseDto> CreateAsync(CreateCustomerDto dto, CancellationToken ct = default);
        Task<CustomerResponseDto> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<CustomerResponseDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
    }
}
