using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class FeatureExtractionService(
        ILogger<FeatureExtractionService> logger,
        IEcgBaselineService baselineService
        /*IFeatureCalculatorService featureCalculatorService*/) : IFeatureExtractionService
    {
        public float[] ExtractFeatures(List<Image<Rgba32>> ecgStrips)
        {
            var allFeatures = new List<float>();
            
            foreach (var strip in ecgStrips)
            {
                int baseline = baselineService.FindBaseline(strip);
                // float[] stripFeatures = featureCalculatorService.CalculateFeatures(strip, baseline);
                // allFeatures.AddRange(stripFeatures);
            }

            logger.LogInformation("Екстракцію завершено. Отримано фінальний вектор з {Count} ознак.", allFeatures.Count);
            return allFeatures.ToArray();
        }
    }
}