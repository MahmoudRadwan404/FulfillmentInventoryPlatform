using FulfillmentInventoryPlatform.Application.DTOs;
using FulfillmentInventoryPlatform.Application.Exceptions;
using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categories;
        private readonly IUnitOfWork _uow;

        public CategoryService(ICategoryRepository categories, IUnitOfWork uow)
        {
            _categories = categories;
            _uow = uow;
        }

        public async Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ValidationException("Category name is required.");

            if (await _categories.NameExistsAsync(dto.Name.Trim(), null, ct))
                throw new ConflictException($"A category named \"{dto.Name}\" already exists.");

            if (dto.ParentCategoryId.HasValue)
            {
                var parent = await _categories.GetByIdAsync(dto.ParentCategoryId.Value, ct)
                    ?? throw new NotFoundException(nameof(Category), dto.ParentCategoryId.Value);

                if (!parent.IsActive)
                    throw new ValidationException("Cannot attach a category to an inactive parent category.");
            }

            var category = new Category
            {
                Name = dto.Name.Trim(),
                Description = dto.Description,
                ParentCategoryId = dto.ParentCategoryId,
                IsActive = true
            };

            _categories.Add(category);
            await _uow.SaveChangesAsync(ct);

            return await GetByIdAsync(category.Id, ct);
        }

        public async Task<CategoryResponseDto> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken ct = default)
        {
            var category = await _categories.GetByIdAsync(id, ct)
                ?? throw new NotFoundException(nameof(Category), id);

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ValidationException("Category name is required.");

            if (await _categories.NameExistsAsync(dto.Name.Trim(), id, ct))
                throw new ConflictException($"A category named \"{dto.Name}\" already exists.");

            if (dto.ParentCategoryId == id)
                throw new ValidationException("A category cannot be its own parent.");

            if (dto.ParentCategoryId.HasValue)
            {
                _ = await _categories.GetByIdAsync(dto.ParentCategoryId.Value, ct)
                    ?? throw new NotFoundException(nameof(Category), dto.ParentCategoryId.Value);
            }

            category.Name = dto.Name.Trim();
            category.Description = dto.Description;
            category.ParentCategoryId = dto.ParentCategoryId;

            _categories.Update(category);
            await _uow.SaveChangesAsync(ct);

            return await GetByIdAsync(category.Id, ct);
        }

        public async Task DeactivateAsync(int id, CancellationToken ct = default)
        {
            var category = await _categories.GetByIdAsync(id, ct)
                ?? throw new NotFoundException(nameof(Category), id);

            // Deactivation does not cascade to sub-categories or products (documented assumption).
            category.IsActive = false;
            _categories.Update(category);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task<CategoryResponseDto> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var category = await _categories.GetByIdAsync(id, ct)
                ?? throw new NotFoundException(nameof(Category), id);

            return Map(category);
        }

        public async Task<List<CategoryResponseDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
        {
            var categories = await _categories.GetAllAsync(includeInactive, ct);
            return categories.Select(Map).ToList();
        }

        private static CategoryResponseDto Map(Category c) => new(
            c.Id, c.Name, c.Description, c.IsActive, c.ParentCategoryId, c.ParentCategory?.Name);
    }
}
