using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class FeatureExtractionService(
        ILogger<FeatureExtractionService> logger,
        IEcgWorkingAreaService workingAreaService) : IFeatureExtractionService
    {
        
        private static readonly Color AreaAboveColor = Color.FromRgb(66, 135, 245); // Синій
        private static readonly Color AreaBelowColor = Color.FromRgb(245, 66, 66); // Червоний

        public async Task<(float[]? features, byte[]? finalImage)> ProcessEcgImageAsync(Stream imageStream)
        {
            try
            {
                // Зберігаємо позицію стріму на випадок потреби повторного читання
                var originalPosition = imageStream.Position;

                using var originalImage = await Image.LoadAsync<Rgba32>(imageStream);
                logger.LogInformation($"Завантажено зображення розміром: {originalImage.Width}x{originalImage.Height}");

                var workingArea = workingAreaService.FindWorkingArea(originalImage);

                // Перевіряємо чи знайдено валідну робочу область
                if (workingArea.Width <= 0 || workingArea.Height <= 0)
                {
                    logger.LogWarning("Робочу область не знайдено, використовуємо все зображення");
                    workingArea = new Rectangle(0, 0, originalImage.Width, originalImage.Height);
                }

                // Додаткова перевірка на розумність розміру робочої області
                var areaPercentage = (double)(workingArea.Width * workingArea.Height) /
                                     (originalImage.Width * originalImage.Height);
                if (areaPercentage < 0.1) // Менше 10% від загального зображення
                {
                    logger.LogWarning(
                        $"Робоча область занадто мала ({areaPercentage:P1}), використовуємо все зображення");
                    workingArea = new Rectangle(0, 0, originalImage.Width, originalImage.Height);
                }

                logger.LogInformation(
                    "Використовується робоча область: {WorkingArea} ({Percentage:P1} від загального зображення)",
                    workingArea, areaPercentage);

                // Обрізаємо зображення
                using var croppedImage = CropImageSafely(originalImage, workingArea);

                // Конвертуємо в байти
                using var memoryStream = new MemoryStream();
                await croppedImage.SaveAsPngAsync(memoryStream);
                byte[] croppedImageBytes = memoryStream.ToArray();

                logger.LogInformation(
                    $"Результуюче зображення: {croppedImage.Width}x{croppedImage.Height}, {croppedImageBytes.Length} байт");

                // Поки що повертаємо порожні features
                var dummyFeatures = Array.Empty<float>();

                return (dummyFeatures, croppedImageBytes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Помилка під час обробки зображення ЕКГ");
                return (null, null);
            }
        }

        private Image<Rgba32> CropImageSafely(Image<Rgba32> image, Rectangle cropArea)
        {
            try
            {
                // Нормалізуємо координати та розміри
                var safeX = Math.Max(0, Math.Min(cropArea.X, image.Width - 1));
                var safeY = Math.Max(0, Math.Min(cropArea.Y, image.Height - 1));
                var safeWidth = Math.Max(1, Math.Min(cropArea.Width, image.Width - safeX));
                var safeHeight = Math.Max(1, Math.Min(cropArea.Height, image.Height - safeY));

                var safeCropArea = new Rectangle(safeX, safeY, safeWidth, safeHeight);

                logger.LogDebug($"Обрізка: оригінал {cropArea} -> безпечна {safeCropArea}");

                var croppedImage = image.Clone();
                croppedImage.Mutate(x => x.Crop(safeCropArea));
                return croppedImage;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Помилка при обрізці, повертаємо копію оригіналу");
                return image.Clone();
            }
        }
    }
}