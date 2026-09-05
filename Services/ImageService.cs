using Microsoft.AspNetCore.Http;

namespace PharmacyAPI.Services;

public class ImageService
{
    private readonly string _uploadPath;

    public ImageService(IConfiguration configuration)
    {
        _uploadPath = configuration["FileStorage:UploadPath"]
            ?? "/var/www/uploads/E_Commerce";
    }

    public async Task<string> SaveImageAsync(
        IFormFile image,
        string folder,
        CancellationToken cancellationToken = default)
    {
        if (image == null || image.Length == 0)
            throw new ArgumentException("Image is required.");

        var extension = Path.GetExtension(image.FileName)
            .ToLowerInvariant();

        var allowedExtensions = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        if (!allowedExtensions.Contains(extension))
            throw new ArgumentException("Invalid image format.");

        var folderPath = Path.Combine(
            _uploadPath,
            folder);

      

        var fileName = $"{Guid.NewGuid():N}{extension}";

        var filePath = Path.Combine(
            folderPath,
            fileName);

        await using var stream = new FileStream(
            filePath,
            FileMode.Create);

        await image.CopyToAsync(
            stream,
            cancellationToken);

        return $"/uploads/E_Commerce/{folder}/{fileName}";
    }
}