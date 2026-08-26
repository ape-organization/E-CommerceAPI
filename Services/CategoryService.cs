using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;

namespace PharmacyAPI.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetCategories();

        Task<Category?> GetCategory(int id);

        Task<Category> CreateCategory(
            CreateCategoryRequest dto
        );

        Task<List<CategoryMenu>> GetCategoriesForMenu();

        Task UpdateCategory(
            int id,
            CreateCategoryRequest dto
        );

        Task DeleteCategory(
            int id,
            Category category
        );
    }


    public class CategoryService : ICategoryService
    {
        private readonly PharmacyDbContext _context;

        private readonly IWebHostEnvironment _environment;


        public CategoryService(
            PharmacyDbContext context,
            IWebHostEnvironment environment
        )
        {
            _context = context;

            _environment = environment;
        }


        // =====================================================
        // GET CATEGORIES FOR MENU
        // =====================================================

        public async Task<List<CategoryMenu>> GetCategoriesForMenu()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Include(c => c.SubCategories)
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Name)
                .ToListAsync();


            return categories.Select(c => new CategoryMenu
            {
                Id = c.Id,

                Name = c.Name,

                SubCategories = c.SubCategories
                    .Where(sc => !sc.IsDeleted)
                    .OrderBy(sc => sc.Name)
                    .Select(sc => new SubCategoryMenuDto
                    {
                        Id = sc.Id,

                        Name = sc.Name
                    })
                    .ToList()

            }).ToList();
        }


        // =====================================================
        // GET ALL CATEGORIES
        // =====================================================

        public async Task<List<Category>> GetCategories()
        {
            return await _context.Categories

                .Where(c => !c.IsDeleted)

                .Include(c => c.SubCategories)

                .ToListAsync();
        }


        // =====================================================
        // GET CATEGORY
        // =====================================================

        public async Task<Category?> GetCategory(int id)
        {
            return await _context.Categories

                .Include(c => c.SubCategories)

                .FirstOrDefaultAsync(
                    c =>
                        c.Id == id &&
                        !c.IsDeleted
                );
        }


        // =====================================================
        // CREATE CATEGORY
        // =====================================================

        public async Task<Category> CreateCategory(
            CreateCategoryRequest dto
        )
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));


            var category = new Category
            {
                Name = dto.Name,

                IsDeleted = false
            };


            // -------------------------------------------------
            // IMAGE
            // -------------------------------------------------

            if (dto.Image != null)
            {
                category.ImageUrl =
                    await SaveImage(dto.Image);
            }


            _context.Categories.Add(category);

            await _context.SaveChangesAsync();


            return category;
        }


        // =====================================================
        // UPDATE CATEGORY
        // =====================================================

        public async Task UpdateCategory(
            int id,
            CreateCategoryRequest dto
        )
        {
            var category =
                await _context.Categories
                    .FirstOrDefaultAsync(
                        c => c.Id == id
                    );


            if (category == null)
                throw new KeyNotFoundException(
                    "Category not found."
                );


            category.Name = dto.Name;


            // -------------------------------------------------
            // NEW IMAGE
            // -------------------------------------------------

            if (dto.Image != null)
            {
                // Delete old image

                DeleteImage(category.ImageUrl);


                // Save new image

                category.ImageUrl =
                    await SaveImage(dto.Image);
            }


            await _context.SaveChangesAsync();
        }


        // =====================================================
        // DELETE CATEGORY
        // =====================================================

        public async Task DeleteCategory(
            int id,
            Category category
        )
        {
            category.IsDeleted = true;

            await _context.SaveChangesAsync();
        }


        // =====================================================
        // SAVE IMAGE
        // =====================================================

        private async Task<string> SaveImage(
            IFormFile image
        )
        {
            if (image == null ||
                image.Length == 0)
            {
                throw new ArgumentException(
                    "Invalid image."
                );
            }


            // -------------------------------------------------
            // ALLOWED EXTENSIONS
            // -------------------------------------------------

            var allowedExtensions =
                new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp",
                    ".jfif"
                };


            var extension =
                Path.GetExtension(
                    image.FileName
                ).ToLowerInvariant();


            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException(
                    "Only JPG, JPEG, PNG and WEBP images are allowed."
                );
            }


            // -------------------------------------------------
            // UPLOAD DIRECTORY
            // -------------------------------------------------

            var uploadsFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "categories"
                );


            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(
                    uploadsFolder
                );
            }


            // -------------------------------------------------
            // UNIQUE FILE NAME
            // -------------------------------------------------

            var fileName =
                $"{Guid.NewGuid()}{extension}";


            var filePath =
                Path.Combine(
                    uploadsFolder,
                    fileName
                );


            // -------------------------------------------------
            // SAVE FILE
            // -------------------------------------------------

            using (
                var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create
                    )
            )
            {
                await image.CopyToAsync(stream);
            }


            // -------------------------------------------------
            // DATABASE PATH
            // -------------------------------------------------

            return
                $"/uploads/categories/{fileName}";
        }


        // =====================================================
        // DELETE IMAGE
        // =====================================================

        private void DeleteImage(
            string? imageUrl
        )
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;


            var relativePath =
                imageUrl.TrimStart(
                    '/',
                    '\\'
                );


            var filePath =
                Path.Combine(
                    _environment.WebRootPath,
                    relativePath
                );


            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}