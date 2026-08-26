using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _db;
        public CustomerRepository(AppDbContext db) => _db = db;

        public Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default) =>
            _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

        public async Task<List<Customer>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
        {
            var query = _db.Customers.AsQueryable();
            if (!includeInactive) query = query.Where(c => c.IsActive);
            return await query.OrderBy(c => c.Name).ToListAsync(ct);
        }

        public void Add(Customer customer) => _db.Customers.Add(customer);
    }
}
