using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Exceptions;
using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _products;
        private readonly ICategoryRepository _categories;
        private readonly IUnitOfWork _uow;

        public ProductService(IProductRepository products, ICategoryRepository categories, IUnitOfWork uow)
        {
            _products = products;
            _categories = categories;
            _uow = uow;
        }

        public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ValidationException("Product name is required.");

            var category = await _categories.GetByIdAsync(dto.CategoryId, ct)
                ?? throw new NotFoundException(nameof(Category), dto.CategoryId);

            if (!category.IsActive)
                throw new ValidationException("Cannot assign a product to an inactive category.");

            if (await _products.NameExistsAsync(dto.Name.Trim(), null, ct))
                throw new ConflictException($"A product named \"{dto.Name}\" already exists.");

            var product = new Product
            {
                Name = dto.Name.Trim(),
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                IsActive = true
            };

            _products.Add(product);
            await _uow.SaveChangesAsync(ct);

            return await GetByIdAsync(product.Id, ct);
        }

        public async Task<ProductResponseDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken ct = default)
        {
            var product = await _products.GetByIdAsync(id, ct)
                ?? throw new NotFoundException(nameof(Product), id);

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ValidationException("Product name is required.");

            var category = await _categories.GetByIdAsync(dto.CategoryId, ct)
                ?? throw new NotFoundException(nameof(Category), dto.CategoryId);

            if (!category.IsActive)
                throw new ValidationException("Cannot assign a product to an inactive category.");

            if (await _products.NameExistsAsync(dto.Name.Trim(), id, ct))
                throw new ConflictException($"A product named \"{dto.Name}\" already exists.");

            product.Name = dto.Name.Trim();
            product.Description = dto.Description;
            product.CategoryId = dto.CategoryId;

            _products.Update(product);
            await _uow.SaveChangesAsync(ct);

            return await GetByIdAsync(product.Id, ct);
        }

        public async Task DeactivateAsync(int id, CancellationToken ct = default)
        {
            var product = await _products.GetByIdAsync(id, ct)
                ?? throw new NotFoundException(nameof(Product), id);

            product.IsActive = false;
            _products.Update(product);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task<ProductResponseDto> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var product = await _products.GetByIdAsync(id, ct)
                ?? throw new NotFoundException(nameof(Product), id);

            return Map(product);
        }

        public async Task<List<ProductResponseDto>> GetAllAsync(bool includeInactive, int? categoryId, string? search, CancellationToken ct = default)
        {
            var products = await _products.GetAllAsync(includeInactive, categoryId, search, ct);
            return products.Select(Map).ToList();
        }

        private static ProductResponseDto Map(Product p) => new(
            p.Id, p.Name, p.Description, p.IsActive, p.CategoryId, p.Category?.Name ?? string.Empty);
    }
}
