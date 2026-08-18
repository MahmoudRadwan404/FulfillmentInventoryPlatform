using FulfillmentInventoryPlatform.Application.Common;
using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence
{
    // Seeds fixed roles and a set of test users (one per role) on startup.
    // Passwords are documented in README.md - change them for anything beyond local/dev use.
    public static class DbInitializer
    {
        public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher)
        {
            await db.Database.MigrateAsync();

            foreach (var roleName in RoleNames.All)
            {
                if (!await db.Roles.AnyAsync(r => r.Name == roleName))
                    db.Roles.Add(new Role { Name = roleName });
            }
            await db.SaveChangesAsync();

            var adminRole = await db.Roles.FirstAsync(r => r.Name == RoleNames.Administrator);
            var operatorRole = await db.Roles.FirstAsync(r => r.Name == RoleNames.WarehouseOperator);
            var managerRole = await db.Roles.FirstAsync(r => r.Name == RoleNames.Manager);

            if (!await db.Users.AnyAsync())
            {
                db.Users.AddRange(
                    new User
                    {
                        Username = "admin",
                        Email = "admin@example.com",
                        PasswordHash = hasher.Hash("Admin@12345"),
                        RoleId = adminRole.Id,
                        IsActive = true
                    },
                    new User
                    {
                        Username = "operator",
                        Email = "operator@example.com",
                        PasswordHash = hasher.Hash("Operator@12345"),
                        RoleId = operatorRole.Id,
                        IsActive = true
                    },
                    new User
                    {
                        Username = "manager",
                        Email = "manager@example.com",
                        PasswordHash = hasher.Hash("Manager@12345"),
                        RoleId = managerRole.Id,
                        IsActive = true
                    }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}
