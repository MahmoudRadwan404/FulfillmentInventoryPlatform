using FulfillmentInventoryPlatform.Application.DTOs;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface IWarehouseService
    {
        Task<WarehouseResponseDto> CreateAsync(CreateWarehouseDto dto, CancellationToken ct = default);
        Task<WarehouseResponseDto> UpdateAsync(int id, UpdateWarehouseDto dto, CancellationToken ct = default);
        Task DeactivateAsync(int id, CancellationToken ct = default);
        Task<WarehouseResponseDto> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<WarehouseResponseDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
    }
}
