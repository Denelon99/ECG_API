using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class FeatureExtractionService(
        ILogger<FeatureExtractionService> logger,
        IEcgWorkingAreaService workingAreaService,
        IEcgStripSplitterService stripSplitterService,
        ICalibrationPulseRemoverService pulseRemoverService,
        IEcgSignalIsolationService signalIsolationService,
        IImageStorageService imageStorageService) : IFeatureExtractionService
    {

        public async Task<(float[]? features, byte[]? finalImage)> ProcessEcgImageAsync(Stream imageStream)
        {
            try
            {
                using var originalImage = await Image.LoadAsync<Rgba32>(imageStream);
               
                // Пошук та обрізка робочої області 
                var workingArea = workingAreaService.FindWorkingArea(originalImage);
                if (workingArea.Width <= 0 || workingArea.Height <= 0)
                {
                    logger.LogWarning("Робочу область не знайдено, використовуємо все зображення");
                    workingArea = new Rectangle(0, 0, originalImage.Width, originalImage.Height);
                }
                
                using var croppedImage = CropImageSafely(originalImage, workingArea);
                
                using var signalOnlyTransparent = signalIsolationService.IsolateSignal(croppedImage);

                // --- Крок 2.1: Накладання сигналу на білий фон ---
                using var signalWithBackground = new Image<Rgba32>(signalOnlyTransparent.Width, signalOnlyTransparent.Height, Color.White);
                signalWithBackground.Mutate(ctx => ctx.DrawImage(signalOnlyTransparent, 1f)); 
                
                // Видалення калібрувального імпульсу 
                using var imageWithoutPulse = pulseRemoverService.RemovePulse(signalWithBackground);
                
                //Розділення на горизонтальні смуги 
                var strips = stripSplitterService.SplitIntoStrips(imageWithoutPulse, 4);
                if (strips.Count == 0)
                {
                    logger.LogError("Не вдалося розділити зображення на смуги.");
                    return (null, null);
                }
                //Повертаємо першу смугу для візуалізації 
                using var firstStrip = strips[0];
                
                
                using var memoryStream = new MemoryStream();
                await imageWithoutPulse.SaveAsPngAsync(memoryStream);
                byte[]? imageWithoutPulseByte = memoryStream.ToArray();
                
                return (Array.Empty<float>(), imageWithoutPulseByte);
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