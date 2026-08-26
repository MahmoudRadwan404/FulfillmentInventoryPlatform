using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Exceptions;
using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Services
{
    // Administrator-only: create/update/deactivate application users and their roles.
    public class UserService : IUserService
    {
        private readonly IUserRepository _users;
        private readonly IRoleRepository _roles;
        private readonly IPasswordHasher _hasher;
        private readonly IUnitOfWork _uow;

        public UserService(IUserRepository users, IRoleRepository roles, IPasswordHasher hasher, IUnitOfWork uow)
        {
            _users = users;
            _roles = roles;
            _hasher = hasher;
            _uow = uow;
        }

        public async Task<UserResponseDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                throw new ValidationException("Username, email, and password are required.");

            if (dto.Password.Length < 8)
                throw new ValidationException("Password must be at least 8 characters.");

            if (await _users.UsernameOrEmailExistsAsync(dto.Username.Trim(), dto.Email.Trim(), ct))
                throw new ConflictException("A user with this username or email already exists.");

            var role = await _roles.GetByNameAsync(dto.RoleName, ct)
                ?? throw new ValidationException($"Unknown role \"{dto.RoleName}\".");

            var user = new User
            {
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim(),
                PasswordHash = _hasher.Hash(dto.Password),
                RoleId = role.Id,
                IsActive = true
            };

            _users.Add(user);
            await _uow.SaveChangesAsync(ct);

            return await GetByIdAsync(user.Id, ct);
        }

        public async Task<UserResponseDto> UpdateAsync(int id, UpdateUserDto dto, CancellationToken ct = default)
        {
            var user = await _users.GetByIdAsync(id, ct)
                ?? throw new NotFoundException(nameof(User), id);

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new ValidationException("Email is required.");

            var role = await _roles.GetByNameAsync(dto.RoleName, ct)
                ?? throw new ValidationException($"Unknown role \"{dto.RoleName}\".");

            user.Email = dto.Email.Trim();
            user.RoleId = role.Id;

            _users.Update(user);
            await _uow.SaveChangesAsync(ct);

            return await GetByIdAsync(user.Id, ct);
        }

        public async Task DeactivateAsync(int id, CancellationToken ct = default)
        {
            var user = await _users.GetByIdAsync(id, ct)
                ?? throw new NotFoundException(nameof(User), id);

            user.IsActive = false;
            _users.Update(user);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task<UserResponseDto> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var user = await _users.GetByIdAsync(id, ct)
                ?? throw new NotFoundException(nameof(User), id);

            return Map(user);
        }

        public async Task<List<UserResponseDto>> GetAllAsync(CancellationToken ct = default)
        {
            var users = await _users.GetAllAsync(ct);
            return users.Select(Map).ToList();
        }

        private static UserResponseDto Map(User u) => new(
            u.Id, u.Username, u.Email, u.Role?.Name ?? string.Empty, u.IsActive, u.CreatedAtUtc);
    }
}
