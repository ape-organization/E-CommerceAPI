using Microsoft.AspNetCore.Http;

namespace PharmacyAPI.Services;

public class ImageService
{
    private static readonly HashSet<string> AllowedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".jfif"
    ];

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
            throw new ArgumentException("الصوره مطلوبه");

        var extension = Path.GetExtension(image.FileName)
            .ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("نوع الصوره ليس متوافر");

        var folderPath = Path.Combine(
            _uploadPath,
            folder);

        Directory.CreateDirectory(folderPath);
        if (extension == ".jfif")
        {
            extension = ".jpg";
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";

        var filePath = Path.Combine(
            folderPath,
            fileName);

        // useAsync makes CopyToAsync do real async I/O instead of
        // blocking a thread-pool thread on each write.
        await using var stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await image.CopyToAsync(
            stream,
            cancellationToken);

        return $"/uploads/E_Commerce/{folder}/{fileName}";
    }
    public void DeleteImage(string imageUrl)
    {
        try
        {
            var relativePath = imageUrl
                .TrimStart('/')
                .Replace(
                    "uploads/E_Commerce/",
                    "",
                    StringComparison.OrdinalIgnoreCase);

            var filePath = Path.Combine(
                "/var/www/uploads/E_Commerce",
                relativePath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"خطا في مسح الصوره '{imageUrl}': {ex.Message}");
        }
    }

}