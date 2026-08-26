using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<Customer>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
        void Add(Customer customer);
    }
}
