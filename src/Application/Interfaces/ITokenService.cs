using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface ITokenService
    {
        (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
    }
}
