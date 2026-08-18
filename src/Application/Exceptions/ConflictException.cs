namespace FulfillmentInventoryPlatform.Application.Exceptions
{
    // Thrown for state conflicts: concurrency clashes, duplicate names, invalid stock operations
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}
