using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _db;
        public RoleRepository(AppDbContext db) => _db = db;

        public Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
            _db.Roles.FirstOrDefaultAsync(r => r.Name == name, ct);

        public Task<List<Role>> GetAllAsync(CancellationToken ct = default) =>
            _db.Roles.OrderBy(r => r.Name).ToListAsync(ct);
    }
}
