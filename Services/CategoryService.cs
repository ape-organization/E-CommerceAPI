using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;

namespace PharmacyAPI.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetCategories(
            CancellationToken cancellationToken = default);

        Task<Category?> GetCategory(
            int id,
            CancellationToken cancellationToken = default);

        Task<Category> CreateCategory(
            CreateCategoryRequest dto,
            CancellationToken cancellationToken = default);

        Task<List<CategoryMenu>> GetCategoriesForMenu(
            CancellationToken cancellationToken = default);

        Task UpdateCategory(
            int id,
            CreateCategoryRequest dto,
            CancellationToken cancellationToken = default);

        Task DeleteCategory(
            int id,
            CancellationToken cancellationToken = default);
    }


    public class CategoryService : ICategoryService
    {
        private readonly PharmacyDbContext _context;
        private readonly IWebHostEnvironment _environment;


        public CategoryService(
            PharmacyDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        // =====================================================
        // GET CATEGORIES FOR MENU
        // =====================================================

        public async Task<List<CategoryMenu>> GetCategoriesForMenu(
            CancellationToken cancellationToken = default)
        {
            return await _context.Categories
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.NameEn)
                .Select(c => new CategoryMenu
                {
                    Id = c.Id,

                    NameEn = c.NameEn,
                    NameAr = c.NameAr,

                    SubCategories = c.SubCategories
                        .Where(sc => !sc.IsDeleted)
                        .OrderBy(sc => sc.NameEn)
                        .Select(sc => new SubCategoryMenuDto
                        {
                            Id = sc.Id,
                            NameEn = sc.NameEn,
                            NameAr = sc.NameAr
                        })
                        .ToList()
                })
                .ToListAsync(cancellationToken);
        }


        // =====================================================
        // GET ALL CATEGORIES
        // =====================================================

        public async Task<List<Category>> GetCategories(
            CancellationToken cancellationToken = default)
        {
            return await _context.Categories
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .Include(c => c.SubCategories)
                .OrderBy(c => c.NameEn)
                .ToListAsync(cancellationToken);
        }


        // =====================================================
        // GET CATEGORY
        // =====================================================

        public async Task<Category?> GetCategory(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Categories
                .AsNoTracking()
                .Where(c =>
                    c.Id == id &&
                    !c.IsDeleted)
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(cancellationToken);
        }


        // =====================================================
        // CREATE CATEGORY
        // =====================================================

        public async Task<Category> CreateCategory(
            CreateCategoryRequest dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var category = new Category
            {
                NameEn = dto.NameEn,
                NameAr = dto.NameAr,
                IsDeleted = false
            };


            // -------------------------------------------------
            // IMAGE
            // -------------------------------------------------

            if (dto.Image is not null)
            {
                category.ImageUrl = await SaveImage(
                    dto.Image,
                    cancellationToken);
            }


            _context.Categories.Add(category);

            await _context.SaveChangesAsync(cancellationToken);

            return category;
        }


        // =====================================================
        // UPDATE CATEGORY
        // =====================================================

        public async Task UpdateCategory(
            int id,
            CreateCategoryRequest dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);


            var category = await _context.Categories
                .FirstOrDefaultAsync(
                    c => c.Id == id && !c.IsDeleted,
                    cancellationToken);


            if (category is null)
            {
                throw new KeyNotFoundException(
                    "Category not found.");
            }


            category.NameEn = dto.NameEn;
            category.NameAr = dto.NameAr;


            string? oldImageUrl = null;


            // -------------------------------------------------
            // NEW IMAGE
            // -------------------------------------------------

            if (dto.Image is not null)
            {
                oldImageUrl = category.ImageUrl;

                category.ImageUrl = await SaveImage(
                    dto.Image,
                    cancellationToken);
            }


            try
            {
                await _context.SaveChangesAsync(
                    cancellationToken);
            }
            catch
            {
                // If database update fails after saving the new
                // image, remove the newly created image.
                if (dto.Image is not null)
                {
                    DeleteImage(category.ImageUrl);
                }

                throw;
            }


            // -------------------------------------------------
            // DELETE OLD IMAGE AFTER SUCCESSFUL DB UPDATE
            // -------------------------------------------------

            if (!string.IsNullOrWhiteSpace(oldImageUrl))
            {
                DeleteImage(oldImageUrl);
            }
        }


        // =====================================================
        // DELETE CATEGORY
        // =====================================================

        public async Task DeleteCategory(
            int id,
            CancellationToken cancellationToken = default)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(
                    c => c.Id == id && !c.IsDeleted,
                    cancellationToken);


            if (category is null)
            {
                throw new KeyNotFoundException(
                    "Category not found.");
            }


            // Soft delete
            category.IsDeleted = true;


            await _context.SaveChangesAsync(
                cancellationToken);
        }


        // =====================================================
        // SAVE IMAGE
        // =====================================================

        private async Task<string> SaveImage(
            IFormFile image,
            CancellationToken cancellationToken)
        {
            if (image.Length == 0)
            {
                throw new ArgumentException(
                    "Invalid image.");
            }


            // -------------------------------------------------
            // ALLOWED EXTENSIONS
            // -------------------------------------------------

            var allowedExtensions = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp",
                ".jfif"
            };


            var extension = Path.GetExtension(
                image.FileName);


            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException(
                    "Only JPG, JPEG, PNG, WEBP and JFIF images are allowed.");
            }


            // -------------------------------------------------
            // UPLOAD DIRECTORY
            // -------------------------------------------------

            var uploadsFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "categories");


            Directory.CreateDirectory(uploadsFolder);


            // -------------------------------------------------
            // UNIQUE FILE NAME
            // -------------------------------------------------

            var fileName =
                $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";


            var filePath = Path.Combine(
                uploadsFolder,
                fileName);


            // -------------------------------------------------
            // SAVE FILE
            // -------------------------------------------------

            await using var stream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024,
                useAsync: true);


            await image.CopyToAsync(
                stream,
                cancellationToken);


            // -------------------------------------------------
            // DATABASE PATH
            // -------------------------------------------------

            return $"/uploads/categories/{fileName}";
        }


        // =====================================================
        // DELETE IMAGE
        // =====================================================

        private void DeleteImage(
            string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;


            var relativePath = imageUrl.TrimStart(
                '/',
                '\\');


            var filePath = Path.Combine(
                _environment.WebRootPath,
                relativePath);


            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Do not fail the database operation because
                // an old image could not be deleted.
            }
        }
    }
}