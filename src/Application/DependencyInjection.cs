using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Application.Services;
using FulfillmentInventoryPlatform.Application.Services.Customers;
using FulfillmentInventoryPlatform.Application.Services.Idempotency;
using FulfillmentInventoryPlatform.Application.Services.Orders;
using Microsoft.Extensions.DependencyInjection;

namespace FulfillmentInventoryPlatform.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IWarehouseService, WarehouseService>();
            services.AddScoped<IStockService, StockService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();

            // Milestone 2
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IOrderProcessingService, OrderProcessingService>();
            services.AddScoped<IIdempotencyService, IdempotencyService>();

            return services;
        }
    }
}
