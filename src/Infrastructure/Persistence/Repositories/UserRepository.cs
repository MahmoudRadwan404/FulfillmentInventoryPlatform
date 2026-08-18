using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;
        public UserRepository(AppDbContext db) => _db = db;

        public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
            _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, ct);

        public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
            _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == username, ct);

        public Task<List<User>> GetAllAsync(CancellationToken ct = default) =>
            _db.Users.Include(u => u.Role).OrderBy(u => u.Username).ToListAsync(ct);

        public Task<bool> UsernameOrEmailExistsAsync(string username, string email, CancellationToken ct = default) =>
            _db.Users.AnyAsync(u => u.Username == username || u.Email == email, ct);

        public void Add(User user) => _db.Users.Add(user);
        public void Update(User user) => _db.Users.Update(user);
    }
}
