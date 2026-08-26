namespace FulfillmentInventoryPlatform.Application.DTOs
{
    public record LoginRequestDto(string Username, string Password);

    public record LoginResponseDto(string Token, DateTime ExpiresAtUtc, string Username, string Role);
}
