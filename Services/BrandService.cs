using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;

namespace PharmacyAPI.Services
{
    public interface IBrandService
    {
        Task<List<Brand>> GetBrands();

        Task<Brand?> GetBrand(int id);

        Task<Brand?> CreateBrand(
            BrandRequest request);

        Task<bool> UpdateBrand(
            int id,
            BrandRequest request);

        Task<bool> DeleteBrand(int id);
    }


    public class BrandService : IBrandService
    {
        private readonly PharmacyDbContext _context;

        private readonly IWebHostEnvironment _environment;


        public BrandService(
            PharmacyDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        // =====================================================
        // GET ALL BRANDS
        // =====================================================

        public async Task<List<Brand>> GetBrands()
        {
            return await _context.Brand

                .Where(b => !b.IsDeleted)

                .OrderBy(b => b.Name)

                .ToListAsync();
        }


        // =====================================================
        // GET BRAND
        // =====================================================

        public async Task<Brand?> GetBrand(int id)
        {
            return await _context.Brand

                .Include(b => b.Products)

                .FirstOrDefaultAsync(
                    b =>
                        b.Id == id &&
                        !b.IsDeleted
                );
        }


        // =====================================================
        // CREATE BRAND
        // =====================================================

        public async Task<Brand?> CreateBrand(
            BrandRequest request)
        {
            if (request == null)
            {
                return null;
            }


            var brand = new Brand
            {
                Name = request.Name.Trim(),

                IsDeleted = false
            };


            // =================================================
            // SAVE IMAGE
            // =================================================

            if (request.Image != null)
            {
                brand.ImageUrl =
                    await SaveImage(
                        request.Image
                    );
            }


            _context.Brand.Add(brand);

            await _context.SaveChangesAsync();


            return brand;
        }


        // =====================================================
        // UPDATE BRAND
        // =====================================================

        public async Task<bool> UpdateBrand(
            int id,
            BrandRequest request)
        {
            var brand =
                await _context.Brand
                    .FirstOrDefaultAsync(
                        b =>
                            b.Id == id &&
                            !b.IsDeleted
                    );


            if (brand == null)
            {
                return false;
            }


            brand.Name =
                request.Name.Trim();


            // =================================================
            // NEW IMAGE
            // =================================================

            if (request.Image != null)
            {
                // Delete old image
                DeleteImage(
                    brand.ImageUrl
                );


                // Save new image
                brand.ImageUrl =
                    await SaveImage(
                        request.Image
                    );
            }


            await _context.SaveChangesAsync();


            return true;
        }


        // =====================================================
        // DELETE BRAND
        // =====================================================

        public async Task<bool> DeleteBrand(int id)
        {
            var brand =
                await _context.Brand
                    .FirstOrDefaultAsync(
                        b => b.Id == id
                    );


            if (brand == null)
            {
                return false;
            }


            brand.IsDeleted = true;


            await _context.SaveChangesAsync();


            return true;
        }


        // =====================================================
        // SAVE IMAGE
        // =====================================================

        private async Task<string> SaveImage(
            IFormFile image)
        {
            var uploadsFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "brands"
                );


            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(
                    uploadsFolder
                );
            }


            var extension =
                Path.GetExtension(
                    image.FileName
                );


            var fileName =
                $"{Guid.NewGuid()}{extension}";


            var filePath =
                Path.Combine(
                    uploadsFolder,
                    fileName
                );


            using (
                var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create
                    )
            )
            {
                await image.CopyToAsync(
                    stream
                );
            }


            return $"/uploads/brands/{fileName}";
        }


        // =====================================================
        // DELETE IMAGE
        // =====================================================

        private void DeleteImage(
            string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return;
            }


            var fileName =
                Path.GetFileName(
                    imageUrl
                );


            var filePath =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "brands",
                    fileName
                );


            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}