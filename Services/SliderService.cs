using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;

namespace PharmacyAPI.Services
{
    public interface ISliderService
    {
        Task<List<Slider>> GetSliders(
            CancellationToken cancellationToken = default);



        Task<Slider> CreateSlider(
            sliderDto dto,
            CancellationToken cancellationToken = default);

      

        Task UpdateSlider(
            int id,
            sliderDto dto,
            CancellationToken cancellationToken = default);

     
    }


    public class SliderService : ISliderService
    {
        private readonly PharmacyDbContext _context;
        private readonly IWebHostEnvironment _environment;


        public SliderService(
            PharmacyDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // =====================================================
        // GET ALL Sliders
        // =====================================================

        public async Task<List<Slider>> GetSliders(
            CancellationToken cancellationToken = default)
        {
            return await _context.Sliders
                .AsNoTracking()
               
                .ToListAsync(cancellationToken);
        }


        // =====================================================
        // GET CATEGORY
        // =====================================================

        public async Task<Slider?> GetSlider(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Sliders
                .AsNoTracking()
              
                .FirstOrDefaultAsync(cancellationToken);
        }


        // =====================================================
        // CREATE CATEGORY
        // =====================================================

        public async Task<Slider> CreateSlider(
            sliderDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var slider = new Slider { };


            // -------------------------------------------------
            // IMAGE
            // -------------------------------------------------

            if (dto.Image is not null)
            {
                slider.ImageUrl = await SaveImage(
                    dto.Image,
                    cancellationToken);
            }


            _context.Sliders.Add(slider);

            await _context.SaveChangesAsync(cancellationToken);

            return slider;
        }


        // =====================================================
        // UPDATE CATEGORY
        // =====================================================

        public async Task UpdateSlider(
            int id,
            sliderDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);


            var slider = await _context.Sliders
                .FirstOrDefaultAsync(
                    c => c.Id == id ,
                    cancellationToken);


            if (slider is null)
            {
                throw new KeyNotFoundException(
                    "Category not found.");
            }



            string? oldImageUrl = null;


            // -------------------------------------------------
            // NEW IMAGE
            // -------------------------------------------------

            if (dto.Image is not null)
            {
                oldImageUrl = slider.ImageUrl;

                slider.ImageUrl = await SaveImage(
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
                    DeleteImage(slider.ImageUrl);
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
                "sliders");


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

            return $"/uploads/sliders/{fileName}";
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