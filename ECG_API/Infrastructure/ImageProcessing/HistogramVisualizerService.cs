using ECG_API.Core.Services;
using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using PointF = SixLabors.ImageSharp.PointF;

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class HistogramVisualizerService(
        IGainLineDetectorService gainLineDetector,
        ILogger<HistogramVisualizerService> logger) : IHistogramVisualizerService
    {
        public Image<Rgba32> CreateVerticalProjectionHistogram(Image<Rgba32> ecgImage)
        {
            try
            {
                var detectionResult = gainLineDetector.DetectGainLines(ecgImage);

                if (detectionResult.Projection == null)
                {
                    logger.LogWarning("Не вдалося отримати проекцію для візуалізації.");
                    return new Image<Rgba32>(1, 1);
                }

                using (detectionResult.Projection)
                {
                    return DrawHistogram(detectionResult.Projection, ecgImage.Width, detectionResult.Threshold);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Помилка під час створення гістограми.");
                return new Image<Rgba32>(1, 1);
            }
        }
        private Image<Rgba32> DrawHistogram(Mat projection, int width, double threshold)
        {
            Cv2.MinMaxLoc(projection, out _, out double maxVal);
            if (maxVal == 0)
            {
                maxVal = 1;
            }

            int histHeight = 256; 
            var histogramImage = new Image<Rgba32>(width, histHeight, Color.White);

            histogramImage.Mutate(ctx =>
            {
                for (int x = 0; x < width; x++)
                {
                    var value = projection.At<int>(0, x);
                    var normalizedHeight = (int)((value / maxVal) * histHeight);
    
                    if (normalizedHeight > 0)
                    {
                        ctx.DrawLine(
                            Color.Black, 
                            1, 
                            new PointF(x, histHeight - 1), 
                            new PointF(x, histHeight - 1 - normalizedHeight)
                        );
                    }
                }

                var thresholdY = histHeight - 1 - (int)((threshold / maxVal) * histHeight);

                if (thresholdY >= 0 && thresholdY < histHeight)
                {
                    ctx.DrawLine(
                        Color.Red, 
                        2, 
                        new PointF(0, thresholdY), 
                        new PointF(width - 1, thresholdY)
                    );
                }
            });

            return histogramImage;
        }
    }
}