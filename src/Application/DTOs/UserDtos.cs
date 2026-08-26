namespace FulfillmentInventoryPlatform.Application.DTOs
{
    public record CreateUserDto(string Username, string Email, string Password, string RoleName);

    public record UpdateUserDto(string Email, string RoleName);

    public record UserResponseDto(int Id, string Username, string Email, string RoleName, bool IsActive, DateTime CreatedAtUtc);
}
