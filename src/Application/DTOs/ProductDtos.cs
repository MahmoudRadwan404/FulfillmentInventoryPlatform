namespace FulfillmentInventoryPlatform.Application.DTOs
{
    public record CreateProductDto(string Name, string? Description, int CategoryId);

    public record UpdateProductDto(string Name, string? Description, int CategoryId);

    public record ProductResponseDto(
        int Id,
        string Name,
        string? Description,
        bool IsActive,
        int CategoryId,
        string CategoryName);
}
