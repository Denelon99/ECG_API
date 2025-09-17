using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class CalibrationPulseRemoverService(
        ILogger<CalibrationPulseRemoverService> logger,
        IGainLineDetectorService gainLineDetector) : ICalibrationPulseRemoverService
    {
        public Image<Rgba32> RemovePulse(Image<Rgba32> image)
        {
            try
            {
                var gainLineCenters = gainLineDetector.DetectGainLineCenters(image);

                if (gainLineCenters.Count == 0)
                {
                    logger.LogWarning("Лінії посилення не знайдено, зображення не змінено.");
                    return image.Clone();
                }

                var resultImage = image.Clone();
                int removalWidth = 10; 

                resultImage.Mutate(ctx =>
                {
                    foreach (var centerX in gainLineCenters)
                    {
                        int startX = Math.Max(0, centerX - (removalWidth / 2));
                        var removalRect = new Rectangle(startX, 0, removalWidth, image.Height);
                        ctx.Fill(Color.White, removalRect);
                    }
                });
                
                logger.LogInformation("Успішно видалено {Count} ліній посилення.", gainLineCenters.Count);
                return resultImage;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Помилка під час видалення ліній посилення.");
                return image.Clone();
            }
        }
    }
}