namespace FulfillmentInventoryPlatform.Application.Common
{
    public static class RoleNames
    {
        public const string Administrator = "Administrator";
        public const string WarehouseOperator = "WarehouseOperator";
        public const string Manager = "Manager";

        public static readonly string[] All = { Administrator, WarehouseOperator, Manager };
    }
}
