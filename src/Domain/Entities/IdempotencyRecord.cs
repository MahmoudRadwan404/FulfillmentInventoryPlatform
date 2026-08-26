using FulfillmentInventoryPlatform.Domain.Common;

namespace FulfillmentInventoryPlatform.Domain.Entities
{
    
    public class IdempotencyRecord : BaseEntity
    {
        public string Key { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;

        public int ResponseStatusCode { get; set; }
        public string ResponseBody { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
