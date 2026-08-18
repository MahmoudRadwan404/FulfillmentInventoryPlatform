using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface IRoleRepository
    {
        Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
        Task<List<Role>> GetAllAsync(CancellationToken ct = default);
    }
}
