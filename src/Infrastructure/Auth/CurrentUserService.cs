using System.Security.Claims;
using FulfillmentInventoryPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FulfillmentInventoryPlatform.Infrastructure.Auth
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CurrentUserService(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public int UserId
        {
            get
            {
                var value = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                return int.TryParse(value, out var id) ? id : 0;
            }
        }

        public string? Username => User?.FindFirstValue(ClaimTypes.Name);

        public string? Role => User?.FindFirstValue(ClaimTypes.Role);
    }
}
