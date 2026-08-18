namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    // Reads the authenticated user id/role from the current HTTP context (JWT claims)
    public interface ICurrentUserService
    {
        int UserId { get; }
        string? Username { get; }
        string? Role { get; }
    }
}
