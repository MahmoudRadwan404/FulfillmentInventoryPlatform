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
                var value = User?.FindFirst(ClaimTypes.NameIdentifier).Value;
                return int.TryParse(value, out var id) ? id : 0;
            }
        }

        public string? Username => User?.FindFirst(ClaimTypes.Name).Value;

        public string? Role => User?.FindFirst(ClaimTypes.Role).Value;
    }
}
