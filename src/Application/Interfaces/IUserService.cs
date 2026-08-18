using FulfillmentInventoryPlatform.Application.DTOs;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserResponseDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default);
        Task<UserResponseDto> UpdateAsync(int id, UpdateUserDto dto, CancellationToken ct = default);
        Task DeactivateAsync(int id, CancellationToken ct = default);
        Task<UserResponseDto> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<UserResponseDto>> GetAllAsync(CancellationToken ct = default);
    }
}
