namespace FulfillmentInventoryPlatform.Application.DTOs
{
    public record CreateWarehouseDto(string Name, string? Location);

    public record UpdateWarehouseDto(string Name, string? Location);

    public record WarehouseResponseDto(int Id, string Name, string? Location, bool IsActive);
}
