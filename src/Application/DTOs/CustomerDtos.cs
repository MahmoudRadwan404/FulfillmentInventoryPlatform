namespace FulfillmentInventoryPlatform.Application.DTOs
{
    public record CreateCustomerDto(string Name, string? Email, string? Phone);

    public record CustomerResponseDto(int Id, string Name, string? Email, string? Phone, bool IsActive);
}
