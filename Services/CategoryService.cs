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
        private readonly IConfiguration _configuration;
        private readonly ImageService _imageService;
        public CategoryService(
            ImageService imageService,
            IConfiguration configuration,
            PharmacyDbContext context,
            IWebHostEnvironment environment)
        {
            _imageService = imageService;
            _configuration = configuration;
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
                category.ImageUrl = await _imageService.SaveImageAsync(
                    dto.Image,"categories",
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
                    "الفئات غير متوفره");
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

                category.ImageUrl = await _imageService.SaveImageAsync(
                    dto.Image,"categories",                    cancellationToken);
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
                    _imageService.DeleteImage(category.ImageUrl);
                }

                throw;
            }


            // -------------------------------------------------
            // DELETE OLD IMAGE AFTER SUCCESSFUL DB UPDATE
            // -------------------------------------------------

            if (!string.IsNullOrWhiteSpace(oldImageUrl))
            {
                _imageService.DeleteImage(oldImageUrl);
            }
        }


        // =====================================================
        // DELETE CATEGORY
        // =====================================================
        public async Task DeleteCategory(
    int id,
    CancellationToken cancellationToken = default)
        {
            // Get category with its subcategories
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(
                    c => c.Id == id,
                    cancellationToken);

            if (category is null)
            {
                throw new KeyNotFoundException("الفئات غير متوفره");
            }

            // ---------------------------------------------------------
            // Save category image path before deleting category
            // ---------------------------------------------------------

            var categoryImageUrl = category.ImageUrl;

            // ---------------------------------------------------------
            // Get all subcategory IDs
            // ---------------------------------------------------------

            var subCategoryIds = category.SubCategories
                .Select(sc => sc.Id)
                .ToList();

            // ---------------------------------------------------------
            // Get ALL products belonging to these subcategories
            // ---------------------------------------------------------

            var products = await _context.Products
                .Include(p => p.SubCategories)
                .Where(p =>
                    p.SubCategories.Any(sc =>
                        subCategoryIds.Contains(sc.Id)))
                .ToListAsync(cancellationToken);

            // Save product image paths before deleting products
            var productImageUrls = products
                .Where(p => !string.IsNullOrWhiteSpace(p.ImageUrl))
                .Select(p => p.ImageUrl!)
                .ToList();

            // ---------------------------------------------------------
            // Remove Product <-> SubCategory relationships
            // ---------------------------------------------------------

            foreach (var product in products)
            {
                product.SubCategories.Clear();
            }

            // ---------------------------------------------------------
            // Delete all products
            // ---------------------------------------------------------

            if (products.Count > 0)
            {
                _context.Products.RemoveRange(products);
            }

            // ---------------------------------------------------------
            // Delete all subcategories
            // ---------------------------------------------------------

            if (category.SubCategories.Count > 0)
            {
                _context.SubCategories.RemoveRange(
                    category.SubCategories);
            }

            // ---------------------------------------------------------
            // Delete category
            // ---------------------------------------------------------

            _context.Categories.Remove(category);

            // ---------------------------------------------------------
            // Save everything to database
            // ---------------------------------------------------------

            await _context.SaveChangesAsync(cancellationToken);

            // ---------------------------------------------------------
            // Delete category image
            // ---------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(categoryImageUrl))
            {
                _imageService.DeleteImage(categoryImageUrl);
            }

            // ---------------------------------------------------------
            // Delete product images
            // ---------------------------------------------------------

            foreach (var imageUrl in productImageUrls)
            {
                _imageService.DeleteImage (imageUrl);
            }
        }
        //   public async Task DeleteCategory(
        //int id,
        //CancellationToken cancellationToken = default)
        //   {
        //       // Get category with its subcategories
        //       var category = await _context.Categories
        //           .Include(c => c.SubCategories)
        //           .FirstOrDefaultAsync(
        //               c => c.Id == id && !c.IsDeleted,
        //               cancellationToken);

        //       if (category is null)
        //       {
        //           throw new KeyNotFoundException("Category not found.");
        //       }

        //       // Get all subcategory IDs under this category
        //       var subCategoryIds = category.SubCategories
        //           .Select(sc => sc.Id)
        //           .ToList();

        //       // Soft delete category
        //       category.IsDeleted = true;

        //       // Soft delete all subcategories
        //       foreach (var subCategory in category.SubCategories)
        //       {
        //           subCategory.IsDeleted = true;
        //       }

        //       // Soft delete all products belonging to
        //       // any subcategory under this category
        //       var products = await _context.Products
        //           .Include(p => p.SubCategories)
        //           .Where(p =>
        //               !p.IsDeleted &&
        //               p.SubCategories.Any(sc =>
        //                   subCategoryIds.Contains(sc.Id)))
        //           .ToListAsync(cancellationToken);

        //       foreach (var product in products)
        //       {
        //           product.IsDeleted = true;
        //       }

        //       await _context.SaveChangesAsync(cancellationToken);
        //   }


        // =====================================================
        // SAVE IMAGE
        // =====================================================

        //    private async Task<string> SaveImage(
        //        IFormFile image,
        //        CancellationToken cancellationToken)
        //    {
        //        if (image.Length == 0)
        //        {
        //            throw new ArgumentException(
        //                "Invalid image.");
        //        }


        //        // -------------------------------------------------
        //        // ALLOWED EXTENSIONS
        //        // -------------------------------------------------

        //        var allowedExtensions = new HashSet<string>(
        //            StringComparer.OrdinalIgnoreCase)
        //        {
        //            ".jpg",
        //            ".jpeg",
        //            ".png",
        //            ".webp",
        //            ".jfif"
        //        };


        //        var extension = Path.GetExtension(
        //            image.FileName);


        //        if (!allowedExtensions.Contains(extension))
        //        {
        //            throw new ArgumentException(
        //                "Only JPG, JPEG, PNG, WEBP and JFIF images are allowed.");
        //        }


        //        // -------------------------------------------------
        //        // UPLOAD DIRECTORY
        //        // -------------------------------------------------

        //        //var uploadsFolder = Path.Combine(
        //        //    _environment.WebRootPath,
        //        //    "uploads",
        //        //    "categories");
        //        var uploadsFolder = Path.Combine(
        //_configuration["FileStorage:UploadPath"]!,
        //"categories");

        //        Directory.CreateDirectory(uploadsFolder);


        //        // -------------------------------------------------
        //        // UNIQUE FILE NAME
        //        // -------------------------------------------------

        //        var fileName =
        //            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";


        //        var filePath = Path.Combine(
        //            uploadsFolder,
        //            fileName);


        //        // -------------------------------------------------
        //        // SAVE FILE
        //        // -------------------------------------------------

        //        await using var stream = new FileStream(
        //            filePath,
        //            FileMode.CreateNew,
        //            FileAccess.Write,
        //            FileShare.None,
        //            bufferSize: 64 * 1024,
        //            useAsync: true);


        //        await image.CopyToAsync(
        //            stream,
        //            cancellationToken);


        //        // -------------------------------------------------
        //        // DATABASE PATH
        //        // -------------------------------------------------

        //        return $"/uploads/E_Commerce/categories/{fileName}";
        //    }


        // =====================================================
        // DELETE IMAGE
        // =====================================================

 //       private void DeleteImage(
 //           string? imageUrl)
 //       {
 //           if (string.IsNullOrWhiteSpace(imageUrl))
 //               return;


 //           var relativePath = imageUrl.TrimStart(
 //               '/',
 //               '\\');


 //           //var filePath = Path.Combine(
 //           //    _environment.WebRootPath,
 //           //    relativePath);
 //           var filePath = Path.Combine(
 //_configuration["FileStorage:UploadPath"]!,
 //relativePath);

 //           try
 //           {
 //               if (File.Exists(filePath))
 //               {
 //                   File.Delete(filePath);
 //               }
 //           }
 //           catch
 //           {
 //               // Do not fail the database operation because
 //               // an old image could not be deleted.
 //           }
 //       }
   
    
    
    }
}