using FulfillmentInventoryPlatform.Application.DTOs;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken ct = default);
    }
}
