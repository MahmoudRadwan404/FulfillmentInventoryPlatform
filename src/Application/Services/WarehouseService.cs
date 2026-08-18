using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Exceptions;
using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IWarehouseRepository _warehouses;
        private readonly IUnitOfWork _uow;

        public WarehouseService(IWarehouseRepository warehouses, IUnitOfWork uow)
        {
            _warehouses = warehouses;
            _uow = uow;
        }

        public async Task<WarehouseResponseDto> CreateAsync(CreateWarehouseDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ValidationException("Warehouse name is required.");

            if (await _warehouses.NameExistsAsync(dto.Name.Trim(), null, ct))
                throw new ConflictException($"A warehouse named \"{dto.Name}\" already exists.");

            var warehouse = new Warehouse
            {
                Name = dto.Name.Trim(),
                Location = dto.Location,
                IsActive = true
            };

            _warehouses.Add(warehouse);
            await _uow.SaveChangesAsync(ct);

            return Map(warehouse);
        }

        public async Task<WarehouseResponseDto> UpdateAsync(int id, UpdateWarehouseDto dto, CancellationToken ct = default)
        {
            var warehouse = await _warehouses.GetByIdAsync(id, ct)
                ?? throw new NotFoundException(nameof(Warehouse), id);

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ValidationException("Warehouse name is required.");

            if (await _warehouses.NameExistsAsync(dto.Name.Trim(), id, ct))
                throw new ConflictException($"A warehouse named \"{dto.Name}\" already exists.");

            warehouse.Name = dto.Name.Trim();
            warehouse.Location = dto.Location;

            _warehouses.Update(warehouse);
            await _uow.SaveChangesAsync(ct);

            return Map(warehouse);
        }

        public async Task DeactivateAsync(int id, CancellationToken ct = default)
        {
            var warehouse = await _warehouses.GetByIdAsync(id, ct)
                ?? throw new NotFoundException(nameof(Warehouse), id);

            warehouse.IsActive = false;
            _warehouses.Update(warehouse);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task<WarehouseResponseDto> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var warehouse = await _warehouses.GetByIdAsync(id, ct)
                ?? throw new NotFoundException(nameof(Warehouse), id);

            return Map(warehouse);
        }

        public async Task<List<WarehouseResponseDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
        {
            var warehouses = await _warehouses.GetAllAsync(includeInactive, ct);
            return warehouses.Select(Map).ToList();
        }

        private static WarehouseResponseDto Map(Warehouse w) => new(w.Id, w.Name, w.Location, w.IsActive);
    }
}
