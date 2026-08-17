using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;

namespace PharmacyAPI.Services
{
    public interface ISubCategoryService
    {
        Task<IEnumerable<SubCategoryDto>> GetAllAsync();

        Task<SubCategoryDto?> GetByIdAsync(int id);

        Task<IEnumerable<SubCategoryDto>> GetByCategoryIdAsync(int categoryId);

        Task<SubCategoryDto> CreateAsync(CrudSubCategoryDto dto);

        Task<bool> UpdateAsync(int id, CrudSubCategoryDto dto);

        Task<bool> DeleteAsync(int id);

        Task<bool> AddProductAsync(int subCategoryId, int productId);

        Task<bool> RemoveProductAsync(int subCategoryId, int productId);

        Task<bool> SetProductsAsync(
            int subCategoryId,
            List<int> productIds);
    }
    public class SubCategoryService    : ISubCategoryService
    {
        private readonly PharmacyDbContext _context;

        public SubCategoryService(PharmacyDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SubCategoryDto>> GetAllAsync()
        {
            return await _context.SubCategories
                .AsNoTracking()
                .Include(sc => sc.Category)
                .Include(sc => sc.Products)
                .Select(sc => new SubCategoryDto
                {
                    Id = sc.Id,
                    Name = sc.Name,
                    CategoryId = sc.CategoryId,
                    CategoryName = sc.Category.Name,
                    ProductIds = sc.Products
                        .Select(p => p.Id)
                        .ToList()
                })
                .ToListAsync();
        }

        public async Task<SubCategoryDto?> GetByIdAsync(int id)
        {
            return await _context.SubCategories
                .AsNoTracking()
                .Include(sc => sc.Category)
                .Include(sc => sc.Products)
                .Where(sc => sc.Id == id)
                .Select(sc => new SubCategoryDto
                {
                    Id = sc.Id,
                    Name = sc.Name,
                    CategoryId = sc.CategoryId,
                    CategoryName = sc.Category.Name,
                    ProductIds = sc.Products
                        .Select(p => p.Id)
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<SubCategoryDto>> GetByCategoryIdAsync(
            int categoryId)
        {
            return await _context.SubCategories
                .AsNoTracking()
                .Include(sc => sc.Category)
                .Include(sc => sc.Products)
                .Where(sc => sc.CategoryId == categoryId)
                .Select(sc => new SubCategoryDto
                {
                    Id = sc.Id,
                    Name = sc.Name,
                    CategoryId = sc.CategoryId,
                    CategoryName = sc.Category.Name,
                    ProductIds = sc.Products
                        .Select(p => p.Id)
                        .ToList()
                })
                .ToListAsync();
        }

        public async Task<SubCategoryDto> CreateAsync(
            CrudSubCategoryDto dto)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == dto.CategoryId);

            if (!categoryExists)
                throw new KeyNotFoundException(
                    "Category not found.");

            var nameExists = await _context.SubCategories
                .AnyAsync(sc =>
                    sc.CategoryId == dto.CategoryId &&
                    sc.Name.ToLower() == dto.Name.ToLower());

            if (nameExists)
                throw new InvalidOperationException(
                    "A subcategory with this name already exists in this category.");

            var subCategory = new SubCategory
            {
                Name = dto.Name.Trim(),
                CategoryId = dto.CategoryId
            };

            _context.SubCategories.Add(subCategory);

            await _context.SaveChangesAsync();

            return (await GetByIdAsync(subCategory.Id))!;
        }

        public async Task<bool> UpdateAsync(
            int id,
            CrudSubCategoryDto dto)
        {
            var subCategory = await _context.SubCategories
                .FirstOrDefaultAsync(sc => sc.Id == id);

            if (subCategory == null)
                return false;

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == dto.CategoryId);

            if (!categoryExists)
                throw new KeyNotFoundException(
                    "Category not found.");

            var nameExists = await _context.SubCategories
                .AnyAsync(sc =>
                    sc.Id != id &&
                    sc.CategoryId == dto.CategoryId &&
                    sc.Name.ToLower() == dto.Name.ToLower());

            if (nameExists)
                throw new InvalidOperationException(
                    "A subcategory with this name already exists in this category.");

            subCategory.Name = dto.Name.Trim();
            subCategory.CategoryId = dto.CategoryId;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var subCategory = await _context.SubCategories
                .FirstOrDefaultAsync(sc => sc.Id == id);

            if (subCategory == null)
                return false;

            subCategory.IsDeleted = true;

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> AddProductAsync(
            int subCategoryId,
            int productId)
        {
            var subCategory = await _context.SubCategories
                .Include(sc => sc.Products)
                .FirstOrDefaultAsync(sc => sc.Id == subCategoryId);

            if (subCategory == null)
                return false;

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                throw new KeyNotFoundException(
                    "Product not found.");

            if (subCategory.Products.Any(p => p.Id == productId))
                return true;

            subCategory.Products.Add(product);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveProductAsync(
            int subCategoryId,
            int productId)
        {
            var subCategory = await _context.SubCategories
                .Include(sc => sc.Products)
                .FirstOrDefaultAsync(sc => sc.Id == subCategoryId);

            if (subCategory == null)
                return false;

            var product = subCategory.Products
                .FirstOrDefault(p => p.Id == productId);

            if (product == null)
                return false;

            subCategory.Products.Remove(product);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SetProductsAsync(
            int subCategoryId,
            List<int> productIds)
        {
            var subCategory = await _context.SubCategories
                .Include(sc => sc.Products)
                .FirstOrDefaultAsync(sc => sc.Id == subCategoryId);

            if (subCategory == null)
                return false;

            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            if (products.Count != productIds.Distinct().Count())
                throw new KeyNotFoundException(
                    "One or more products were not found.");

            subCategory.Products.Clear();

            foreach (var product in products)
            {
                subCategory.Products.Add(product);
            }

            await _context.SaveChangesAsync();

            return true;
        }
    }
}

