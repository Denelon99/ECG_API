using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class FeatureExtractionService(
        ILogger<FeatureExtractionService> logger,
        IEcgBaselineService baselineService,
        IFeatureCalculatorService featureCalculatorService) : IFeatureExtractionService
    {
        public float[] ExtractFeatures(List<Image<Rgba32>> ecgStrips)
        {
            var allFeatures = new List<float>();
            
            foreach (var strip in ecgStrips)
            {
                int baseline = baselineService.FindBaseline(strip);
                
                if (baseline == -1)
                {
                    logger.LogWarning("Не вдалося знайти базову лінію для смуги #{StripNumber}. Ознаки будуть нульовими.", strip);
                }
                
                float[] stripFeatures = featureCalculatorService.CalculateFeatures(strip, baseline);
                
                allFeatures.AddRange(stripFeatures);
            }

            return allFeatures.ToArray();
        }
    }
}