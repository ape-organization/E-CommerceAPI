using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;

namespace PharmacyAPI.Services
{
    public interface IBrandService
    {
        Task<List<Brand>> GetBrands(
            CancellationToken cancellationToken = default);

        Task<Brand?> GetBrand(
            int id,
            CancellationToken cancellationToken = default);

        Task<Brand> CreateBrand(
            BrandRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateBrand(
            int id,
            BrandRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteBrand(
            int id,
            CancellationToken cancellationToken = default);
    }


    public class BrandService : IBrandService
    {
        private readonly PharmacyDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ImageService _imageService;

        public BrandService(
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
        // GET ALL BRANDS
        // =====================================================

        public async Task<List<Brand>> GetBrands(
            CancellationToken cancellationToken = default)
        {
            return await _context.Brand
                .AsNoTracking()
                .Where(b => !b.IsDeleted)
                .OrderBy(b => b.NameEn)
                .ToListAsync(cancellationToken);
        }


        // =====================================================
        // GET BRAND
        // =====================================================

        public async Task<Brand?> GetBrand(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Brand
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    b =>
                        b.Id == id &&
                        !b.IsDeleted,
                    cancellationToken);
        }


        // =====================================================
        // CREATE BRAND
        // =====================================================

        public async Task<Brand> CreateBrand(
            BrandRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);


            var brand = new Brand
            {
                NameEn = request.NameEn?.Trim() ?? string.Empty,
                NameAr = request.NameAr?.Trim() ?? string.Empty,
                IsDeleted = false
            };


            // -------------------------------------------------
            // IMAGE
            // -------------------------------------------------

            if (request.Image is not null)
            {
                brand.ImageUrl = await _imageService.SaveImageAsync(
                    request.Image,"brands",
                    cancellationToken);
            }


            _context.Brand.Add(brand);

            await _context.SaveChangesAsync(
                cancellationToken);


            return brand;
        }


        // =====================================================
        // UPDATE BRAND
        // =====================================================

        public async Task<bool> UpdateBrand(
            int id,
            BrandRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);


            var brand = await _context.Brand
                .FirstOrDefaultAsync(
                    b =>
                        b.Id == id &&
                        !b.IsDeleted,
                    cancellationToken);


            if (brand is null)
            {
                return false;
            }


            brand.NameEn =
                request.NameEn?.Trim() ?? string.Empty;

            brand.NameAr =
                request.NameAr?.Trim() ?? string.Empty;


            string? oldImageUrl = null;


            // -------------------------------------------------
            // NEW IMAGE
            // -------------------------------------------------

            if (request.Image is not null)
            {
                oldImageUrl = brand.ImageUrl;

                brand.ImageUrl = await _imageService.SaveImageAsync(
                    request.Image,"brands",
                    cancellationToken);
            }


            try
            {
                await _context.SaveChangesAsync(
                    cancellationToken);
            }
            catch
            {
                // Database update failed.
                // Remove the newly created image.
                if (request.Image is not null)
                {
                    DeleteImage(brand.ImageUrl);
                }

                throw;
            }


            // -------------------------------------------------
            // DELETE OLD IMAGE AFTER DB SUCCESS
            // -------------------------------------------------

            if (!string.IsNullOrWhiteSpace(oldImageUrl))
            {
                DeleteImage(oldImageUrl);
            }


            return true;
        }


        // =====================================================
        // DELETE BRAND
        // =====================================================

        public async Task<bool> DeleteBrand(
            int id,
            CancellationToken cancellationToken = default)
        {
            var brand = await _context.Brand
                .FirstOrDefaultAsync(
                    b =>
                        b.Id == id &&
                        !b.IsDeleted,
                    cancellationToken);


            if (brand is null)
            {
                return false;
            }


            // Soft delete
            brand.IsDeleted = true;


            await _context.SaveChangesAsync(
                cancellationToken);


            return true;
        }


        // =====================================================
        // SAVE IMAGE
        // =====================================================

   //     private async Task<string> SaveImage(
   //         IFormFile image,
   //         CancellationToken cancellationToken)
   //     {
   //         if (image.Length == 0)
   //         {
   //             throw new ArgumentException(
   //                 "Invalid image.");
   //         }


   //         // -------------------------------------------------
   //         // ALLOWED EXTENSIONS
   //         // -------------------------------------------------

   //         var allowedExtensions = new HashSet<string>(
   //             StringComparer.OrdinalIgnoreCase)
   //         {
   //             ".jpg",
   //             ".jpeg",
   //             ".png",
   //             ".webp",
   //             ".jfif"
   //         };


   //         var extension =
   //             Path.GetExtension(image.FileName);


   //         if (!allowedExtensions.Contains(extension))
   //         {
   //             throw new ArgumentException(
   //                 "Only JPG, JPEG, PNG, WEBP and JFIF images are allowed.");
   //         }


   //         // -------------------------------------------------
   //         // UPLOAD DIRECTORY
   //         // -------------------------------------------------

   //         //var uploadsFolder = Path.Combine(
   //         //    _environment.WebRootPath,
   //         //    "uploads",
   //         //    "brands");
   //         var uploadsFolder = Path.Combine(
   //_configuration["FileStorage:UploadPath"]!,
   //"brands");

   //         Directory.CreateDirectory(
   //             uploadsFolder);


   //         // -------------------------------------------------
   //         // UNIQUE FILE NAME
   //         // -------------------------------------------------

   //         var fileName =
   //             $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";


   //         var filePath = Path.Combine(
   //             uploadsFolder,
   //             fileName);


   //         // -------------------------------------------------
   //         // SAVE FILE
   //         // -------------------------------------------------

   //         await using var stream = new FileStream(
   //             filePath,
   //             FileMode.CreateNew,
   //             FileAccess.Write,
   //             FileShare.None,
   //             bufferSize: 64 * 1024,
   //             useAsync: true);


   //         await image.CopyToAsync(
   //             stream,
   //             cancellationToken);


   //         // -------------------------------------------------
   //         // DATABASE PATH
   //         // -------------------------------------------------

   //         return $"/uploads/E_Commerce/brands/{fileName}";
   //     }


        // =====================================================
        // DELETE IMAGE
        // =====================================================

        private void DeleteImage(
            string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }


            var fileName =
                Path.GetFileName(imageUrl);


            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }


            //var filePath = Path.Combine(
            //    _environment.WebRootPath,
            //    "uploads",
            //    "brands",
            //    fileName);
            var filePath = Path.Combine(
    _configuration["FileStorage:UploadPath"]!,
    "brands", fileName);

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Image deletion should not cause
                // the database operation to fail.
            }
        }
    }
}