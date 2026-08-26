namespace FulfillmentInventoryPlatform.Application.Exceptions
{
    public class UnauthorizedAppException : Exception
    {
        public UnauthorizedAppException(string message) : base(message) { }
    }
}
