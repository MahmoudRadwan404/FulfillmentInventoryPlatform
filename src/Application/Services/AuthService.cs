using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Exceptions;
using FulfillmentInventoryPlatform.Application.Interfaces;

namespace FulfillmentInventoryPlatform.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher _hasher;
        private readonly ITokenService _tokenService;

        public AuthService(IUserRepository users, IPasswordHasher hasher, ITokenService tokenService)
        {
            _users = users;
            _hasher = hasher;
            _tokenService = tokenService;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                throw new ValidationException("Username and password are required.");

            var user = await _users.GetByUsernameAsync(dto.Username.Trim(), ct);

            // Same error for "not found" and "wrong password" to avoid leaking which usernames exist.
            if (user is null || !user.IsActive || !_hasher.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAppException("Invalid username or password.");

            var (token, expires) = _tokenService.GenerateToken(user);
            return new LoginResponseDto(token, expires, user.Username, user.Role.Name);
        }
    }
}
