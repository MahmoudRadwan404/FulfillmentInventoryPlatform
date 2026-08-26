using FulfillmentInventoryPlatform.Application.DTOs;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface IProductService
    {
        Task<ProductResponseDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default);
        Task<ProductResponseDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken ct = default);
        Task DeactivateAsync(int id, CancellationToken ct = default);
        Task<ProductResponseDto> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<ProductResponseDto>> GetAllAsync(bool includeInactive, int? categoryId, string? search, CancellationToken ct = default);
    }
}
