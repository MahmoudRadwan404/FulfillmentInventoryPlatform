using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Exceptions;
using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Services.Customers
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customers;
        private readonly IUnitOfWork _uow;

        public CustomerService(ICustomerRepository customers, IUnitOfWork uow)
        {
            _customers = customers;
            _uow = uow;
        }

        public async Task<CustomerResponseDto> CreateAsync(CreateCustomerDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ValidationException("Customer name is required.");

            var customer = new Customer { Name = dto.Name.Trim(), Email = dto.Email, Phone = dto.Phone };
            _customers.Add(customer);
            await _uow.SaveChangesAsync(ct);

            return Map(customer);
        }

        public async Task<CustomerResponseDto> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var customer = await _customers.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Customer), id);
            return Map(customer);
        }

        public async Task<List<CustomerResponseDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
        {
            var customers = await _customers.GetAllAsync(includeInactive, ct);
            return customers.Select(Map).ToList();
        }

        private static CustomerResponseDto Map(Customer c) => new(c.Id, c.Name, c.Email, c.Phone, c.IsActive);
    }
}
