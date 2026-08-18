namespace FulfillmentInventoryPlatform.Application.Exceptions
{
    // Thrown for invalid business input (manual validation, not FluentValidation)
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
}
