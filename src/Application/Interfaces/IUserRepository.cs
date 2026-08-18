using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
        Task<List<User>> GetAllAsync(CancellationToken ct = default);
        Task<bool> UsernameOrEmailExistsAsync(string username, string email, CancellationToken ct = default);
        void Add(User user);
        void Update(User user);
    }
}
