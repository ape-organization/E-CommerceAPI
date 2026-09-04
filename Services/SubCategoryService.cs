using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;

namespace PharmacyAPI.Services
{
    public interface ISubCategoryService
    {
        Task<List<SubCategory>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task<SubCategoryDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<List<SubCategoryDto>> GetByCategoryIdAsync(
            int categoryId,
            CancellationToken cancellationToken = default);

        Task<SubCategoryDto> CreateAsync(
            CrudSubCategoryDto dto,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(
            int id,
            CrudSubCategoryDto dto,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<bool> AddProductAsync(
            int subCategoryId,
            int productId,
            CancellationToken cancellationToken = default);

        Task<bool> RemoveProductAsync(
            int subCategoryId,
            int productId,
            CancellationToken cancellationToken = default);

        Task<bool> SetProductsAsync(
            int subCategoryId,
            List<int> productIds,
            CancellationToken cancellationToken = default);
    }


    public class SubCategoryService : ISubCategoryService
    {
        private readonly PharmacyDbContext _context;


        public SubCategoryService(
            PharmacyDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // GET ALL
        // =====================================================

        public async Task<List<SubCategory>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.SubCategories
                .AsNoTracking()
                .Where(sc =>
                    !sc.IsDeleted )
              .ToListAsync(cancellationToken);
        }


        // =====================================================
        // GET BY ID
        // =====================================================

        public async Task<SubCategoryDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.SubCategories
                .AsNoTracking()
                .Where(sc =>
                    sc.Id == id &&
                    !sc.IsDeleted &&
                    !sc.Category.IsDeleted)
                .Select(sc => new SubCategoryDto
                {
                    Id = sc.Id,

                    NameAr = sc.NameAr,
                    NameEn = sc.NameEn,

                    CategoryId = sc.CategoryId,

                    CategoryNameAr = sc.Category.NameAr,
                    CategoryNameEn = sc.Category.NameEn,

                    ProductIds = sc.Products
                        .Where(p => !p.IsDeleted)
                        .Select(p => p.Id)
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);
        }


        // =====================================================
        // GET BY CATEGORY
        // =====================================================

        public async Task<List<SubCategoryDto>> GetByCategoryIdAsync(
            int categoryId,
            CancellationToken cancellationToken = default)
        {
            return await _context.SubCategories
                .AsNoTracking()
                .Where(sc =>
                    sc.CategoryId == categoryId &&
                    !sc.IsDeleted &&
                    !sc.Category.IsDeleted)
                .OrderBy(sc => sc.NameEn)
                .Select(sc => new SubCategoryDto
                {
                    Id = sc.Id,

                    NameAr = sc.NameAr,
                    NameEn = sc.NameEn,

                    CategoryId = sc.CategoryId,

                    CategoryNameAr = sc.Category.NameAr,
                    CategoryNameEn = sc.Category.NameEn,

                    ProductIds = sc.Products
                        .Where(p => !p.IsDeleted)
                        .Select(p => p.Id)
                        .ToList()
                })
                .ToListAsync(cancellationToken);
        }


        // =====================================================
        // CREATE
        // =====================================================

        public async Task<SubCategoryDto> CreateAsync(
            CrudSubCategoryDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var nameEn = dto.NameEn?.Trim() ?? string.Empty;
            var nameAr = dto.NameAr?.Trim() ?? string.Empty;


            // -------------------------------------------------
            // GET CATEGORY
            // -------------------------------------------------

            var category = await _context.Categories
                .AsNoTracking()
                .Where(c =>
                    c.Id == dto.CategoryId &&
                    !c.IsDeleted)
                .Select(c => new
                {
                    c.Id,
                    c.NameEn,
                    c.NameAr
                })
                .FirstOrDefaultAsync(cancellationToken);


            if (category is null)
            {
                throw new KeyNotFoundException(
                    "Category not found.");
            }


            // -------------------------------------------------
            // CHECK DUPLICATE
            // -------------------------------------------------

            var nameExists = await _context.SubCategories
                .AsNoTracking()
                .AnyAsync(
                    sc =>
                        sc.CategoryId == dto.CategoryId &&
                        !sc.IsDeleted &&
                        sc.NameEn == nameEn &&
                        sc.NameAr == nameAr,
                    cancellationToken);


            if (nameExists)
            {
                throw new InvalidOperationException(
                    "A subcategory with this name already exists in this category.");
            }


            // -------------------------------------------------
            // CREATE
            // -------------------------------------------------

            var subCategory = new SubCategory
            {
                NameAr = nameAr,
                NameEn = nameEn,
                CategoryId = dto.CategoryId,
                IsDeleted = false
            };


            _context.SubCategories.Add(subCategory);

            await _context.SaveChangesAsync(
                cancellationToken);


            return new SubCategoryDto
            {
                Id = subCategory.Id,

                NameAr = subCategory.NameAr,
                NameEn = subCategory.NameEn,

                CategoryId = category.Id,

                CategoryNameAr = category.NameAr,
                CategoryNameEn = category.NameEn,

                ProductIds = new List<int>()
            };
        }
        // =====================================================
        // UPDATE
        // =====================================================

        public async Task<bool> UpdateAsync(
            int id,
            CrudSubCategoryDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);


            var subCategory = await _context.SubCategories
                .FirstOrDefaultAsync(
                    sc =>
                        sc.Id == id &&
                        !sc.IsDeleted,
                    cancellationToken);


            if (subCategory is null)
            {
                return false;
            }


            var nameEn = dto.NameEn?.Trim() ?? string.Empty;
            var nameAr = dto.NameAr?.Trim() ?? string.Empty;


            // -------------------------------------------------
            // CHECK CATEGORY
            // -------------------------------------------------

            var categoryExists = await _context.Categories
                .AsNoTracking()
                .AnyAsync(
                    c =>
                        c.Id == dto.CategoryId &&
                        !c.IsDeleted,
                    cancellationToken);


            if (!categoryExists)
            {
                throw new KeyNotFoundException(
                    "Category not found.");
            }


            // -------------------------------------------------
            // CHECK DUPLICATE
            // -------------------------------------------------

            var nameExists = await _context.SubCategories
                .AsNoTracking()
                .AnyAsync(
                    sc =>
                        sc.Id != id &&
                        sc.CategoryId == dto.CategoryId &&
                        !sc.IsDeleted &&
                        sc.NameEn == nameEn &&
                        sc.NameAr == nameAr,
                    cancellationToken);


            if (nameExists)
            {
                throw new InvalidOperationException(
                    "A subcategory with this name already exists in this category.");
            }


            // -------------------------------------------------
            // UPDATE
            // -------------------------------------------------

            subCategory.NameAr = nameAr;
            subCategory.NameEn = nameEn;
            subCategory.CategoryId = dto.CategoryId;


            await _context.SaveChangesAsync(
                cancellationToken);


            return true;
        }


        // =====================================================
        // DELETE
        // =====================================================

        public async Task<bool> DeleteAsync(
      int id,
      CancellationToken cancellationToken = default)
        {
            var subCategory = await _context.SubCategories
                .FirstOrDefaultAsync(
                    sc =>
                        sc.Id == id &&
                        !sc.IsDeleted,
                    cancellationToken);

            if (subCategory is null)
            {
                return false;
            }

            // Get all active products that belong to this subcategory
            var products = await _context.Products
                .Include(p => p.SubCategories)
                .Where(p =>
                    !p.IsDeleted &&
                    p.SubCategories.Any(sc => sc.Id == id))
                .ToListAsync(cancellationToken);

            // Soft delete the subcategory
            subCategory.IsDeleted = true;

            foreach (var product in products)
            {
                // Remove the deleted subcategory from the product
                product.SubCategories.Remove(subCategory);

                // If the product has no other subcategories,
                // soft delete the product
                if (!product.SubCategories.Any(sc => !sc.IsDeleted))
                {
                    product.IsDeleted = true;
                }
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        // =====================================================
        // ADD PRODUCT
        // =====================================================

        public async Task<bool> AddProductAsync(
            int subCategoryId,
            int productId,
            CancellationToken cancellationToken = default)
        {
            // -------------------------------------------------
            // CHECK SUBCATEGORY
            // -------------------------------------------------

            var subCategoryExists =
                await _context.SubCategories
                    .AsNoTracking()
                    .AnyAsync(
                        sc =>
                            sc.Id == subCategoryId &&
                            !sc.IsDeleted,
                        cancellationToken);


            if (!subCategoryExists)
            {
                return false;
            }


            // -------------------------------------------------
            // CHECK PRODUCT
            // -------------------------------------------------

            var productExists =
                await _context.Products
                    .AsNoTracking()
                    .AnyAsync(
                        p =>
                            p.Id == productId &&
                            !p.IsDeleted,
                        cancellationToken);


            if (!productExists)
            {
                throw new KeyNotFoundException(
                    "Product not found.");
            }


            // -------------------------------------------------
            // CHECK RELATIONSHIP
            // -------------------------------------------------

            var relationshipExists =
                await _context.SubCategories
                    .Where(sc => sc.Id == subCategoryId)
                    .SelectMany(sc => sc.Products)
                    .AnyAsync(
                        p => p.Id == productId,
                        cancellationToken);


            if (relationshipExists)
            {
                return true;
            }


            // -------------------------------------------------
            // LOAD ONLY THE REQUIRED ENTITIES
            // -------------------------------------------------

            var subCategory = await _context.SubCategories
                .FirstAsync(
                    sc => sc.Id == subCategoryId,
                    cancellationToken);


            var product = await _context.Products
                .FirstAsync(
                    p => p.Id == productId,
                    cancellationToken);


            subCategory.Products.Add(product);


            await _context.SaveChangesAsync(
                cancellationToken);


            return true;
        }


        // =====================================================
        // REMOVE PRODUCT
        // =====================================================

        public async Task<bool> RemoveProductAsync(
            int subCategoryId,
            int productId,
            CancellationToken cancellationToken = default)
        {
            var subCategory = await _context.SubCategories
                .Include(sc => sc.Products)
                .FirstOrDefaultAsync(
                    sc =>
                        sc.Id == subCategoryId &&
                        !sc.IsDeleted,
                    cancellationToken);


            if (subCategory is null)
            {
                return false;
            }


            var product = subCategory.Products
                .FirstOrDefault(
                    p =>
                        p.Id == productId &&
                        !p.IsDeleted);


            if (product is null)
            {
                return false;
            }


            subCategory.Products.Remove(product);


            await _context.SaveChangesAsync(
                cancellationToken);


            return true;
        }


        // =====================================================
        // SET PRODUCTS
        // =====================================================

        public async Task<bool> SetProductsAsync(
            int subCategoryId,
            List<int> productIds,
            CancellationToken cancellationToken = default)
        {
            productIds ??= new List<int>();


            var distinctProductIds = productIds
                .Distinct()
                .ToList();


            // -------------------------------------------------
            // CHECK SUBCATEGORY
            // -------------------------------------------------

            var subCategory = await _context.SubCategories
                .Include(sc => sc.Products)
                .FirstOrDefaultAsync(
                    sc =>
                        sc.Id == subCategoryId &&
                        !sc.IsDeleted,
                    cancellationToken);


            if (subCategory is null)
            {
                return false;
            }


            // -------------------------------------------------
            // GET PRODUCTS
            // -------------------------------------------------

            var products = await _context.Products
                .Where(p =>
                    distinctProductIds.Contains(p.Id) &&
                    !p.IsDeleted)
                .ToListAsync(cancellationToken);


            if (products.Count != distinctProductIds.Count)
            {
                throw new KeyNotFoundException(
                    "One or more products were not found.");
            }


            // -------------------------------------------------
            // UPDATE RELATIONSHIPS
            // -------------------------------------------------

            subCategory.Products.Clear();


            foreach (var product in products)
            {
                subCategory.Products.Add(product);
            }


            await _context.SaveChangesAsync(
                cancellationToken);


            return true;
        }
    }
}