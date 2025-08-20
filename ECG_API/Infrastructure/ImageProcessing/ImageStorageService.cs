using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
namespace ECG_API.Infrastructure.ImageProcessing;

public class ImageStorageService(IWebHostEnvironment webHostEnvironment, ILogger<ImageStorageService> logger)
    : IImageStorageService
{
    private readonly string _baseStoragePath = Path.Combine(webHostEnvironment.ContentRootPath, "ProcessedImages");
    public async Task<string> SaveImageAsync(Image<Rgba32> image, string fileName, string? subfolder = null)
    {
        try
        {
            var targetDirectory = _baseStoragePath;

            // Якщо вказано підпапку, додаємо її до шляху
            if (!string.IsNullOrWhiteSpace(subfolder))
            {
                targetDirectory = Path.Combine(_baseStoragePath, subfolder);
            }

            // Створюємо папку, якщо її не існує
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            // Формуємо повний шлях до файлу з розширенням .png
            var fullPath = Path.Combine(targetDirectory, $"{fileName}.png");

            // Асинхронно зберігаємо зображення
            await image.SaveAsPngAsync(fullPath);
                
            logger.LogInformation("Зображення успішно збережено за шляхом: {FullPath}", fullPath);
            return fullPath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Помилка під час збереження зображення {FileName}", fileName);
            throw; // Перекидаємо виняток, щоб вищий рівень знав про помилку
        }
    }
}