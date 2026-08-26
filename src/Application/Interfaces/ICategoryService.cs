using FulfillmentInventoryPlatform.Application.DTOs;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default);
        Task<CategoryResponseDto> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken ct = default);
        Task DeactivateAsync(int id, CancellationToken ct = default);
        Task<CategoryResponseDto> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<CategoryResponseDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
    }
}
