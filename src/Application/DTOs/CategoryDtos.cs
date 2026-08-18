namespace FulfillmentInventoryPlatform.Application.DTOs
{
    public record CreateCategoryDto(string Name, string? Description, int? ParentCategoryId);

    public record UpdateCategoryDto(string Name, string? Description, int? ParentCategoryId);

    public record CategoryResponseDto(
        int Id,
        string Name,
        string? Description,
        bool IsActive,
        int? ParentCategoryId,
        string? ParentCategoryName);
}
