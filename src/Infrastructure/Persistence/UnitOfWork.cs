using FulfillmentInventoryPlatform.Application.Exceptions;
using FulfillmentInventoryPlatform.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;
        public UnitOfWork(AppDbContext db) => _db = db;

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            try
            {
                return await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Translate the EF-specific exception into an Application-layer one so
                // upper layers never need to reference EF Core directly.
                throw new ConcurrencyConflictException(
                    "The record was modified by another request. Please retry.");
            }
        }

        public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
        {
            // Every stock-mutating operation (assign / adjust) runs inside a single
            // DB transaction: either every write (WarehouseStock update + StockAdjustment
            // insert) commits together, or none of it does - no partial updates.
            var strategy = _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    await action();
                    await transaction.CommitAsync(ct);
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            });
        }
    }
}
