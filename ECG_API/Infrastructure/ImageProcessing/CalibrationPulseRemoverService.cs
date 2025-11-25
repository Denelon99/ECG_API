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
                var detectionResult = gainLineDetector.DetectGainLines(image);

                if (detectionResult.Centers.Count == 0)
                {
                    return image.Clone();
                }

                var resultImage = image.Clone();
                int removalWidth = 10; 

                resultImage.Mutate(ctx =>
                {
                    foreach (var centerX in detectionResult.Centers)
                    {
                        int startX = Math.Max(0, centerX - (removalWidth / 2));
                        var removalRect = new Rectangle(startX, 0, removalWidth, image.Height);
                        ctx.Fill(Color.White, removalRect);
                    }
                });
                
                return resultImage;
            }
            catch (Exception ex)
            {
                return image.Clone();
            }
        }
    }
}