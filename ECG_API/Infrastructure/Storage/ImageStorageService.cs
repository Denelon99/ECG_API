using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Infrastructure.Storage;

public class ImageStorageService(IWebHostEnvironment webHostEnvironment, ILogger<ImageStorageService> logger)
    : IImageStorageService
{
    private readonly string _baseStoragePath = Path.Combine(webHostEnvironment.ContentRootPath, "ProcessedImages");

    public async Task<string> SaveImageAsync(Image<Rgba32> image, string fileName, string? subfolder = null)
    {
        var targetDirectory = _baseStoragePath;

        if (!string.IsNullOrWhiteSpace(subfolder))
        {
            targetDirectory = Path.Combine(_baseStoragePath, subfolder);
        }

        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        var fullPath = Path.Combine(targetDirectory, $"{fileName}.png");

        await image.SaveAsPngAsync(fullPath);

        return fullPath;
    }

    public async Task<List<string>> SaveImageStripsAsync(List<Image<Rgba32>> images, string baseFileName,
        string? subfolder = null)
    {
        var savedFilePaths = new List<string>();
        if (images == null || images.Count == 0)
        {
            logger.LogWarning("Спроба зберегти порожній список зображень.");
            return savedFilePaths;
        }

        for (int i = 0; i < images.Count; i++)
        {
            var fileName = $"{baseFileName}_strip_{i}";

            var filePath = await SaveImageAsync(images[i], fileName, subfolder);
            savedFilePaths.Add(filePath);
        }

        return savedFilePaths;
    }

    public void DeleteUserImage(int userId)
    {
        var userDirectory = Path.Combine(_baseStoragePath, userId.ToString());
        if (!Directory.Exists(userDirectory))
            return;
        try
        {
            Directory.Delete(userDirectory, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не вдалося видалити зображення користувача {UserId}", userId);
        }
    }

}
