namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    // Wraps EF Core SaveChanges + transaction boundary so multi-step
    // operations (e.g. stock adjustment) commit or roll back as a whole.
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);

        Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);
    }
}
