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
            
            logger.LogInformation("--- Початок виділення ознак з {Count} смуг ---", ecgStrips.Count);
            
            foreach (var strip in ecgStrips)
            {
                int baseline = baselineService.FindBaseline(strip);
                
                if (baseline == -1)
                {
                    logger.LogWarning("    Не вдалося знайти базову лінію для смуги #{StripNumber}. Ознаки будуть нульовими.", strip);
                }
                
                float[] stripFeatures = featureCalculatorService.CalculateFeatures(strip, baseline);
                
                logger.LogInformation("    Розраховано {FeatureCount} ознак. Приклад: [{F1}, {F2}, ...]", 
                    stripFeatures.Length, 
                    stripFeatures.Length > 0 ? stripFeatures[0] : 0, 
                    stripFeatures.Length > 1 ? stripFeatures[1] : 0);
                
                allFeatures.AddRange(stripFeatures);
            }

            logger.LogInformation("Екстракцію завершено. Отримано фінальний вектор з {Count} ознак.", allFeatures.Count);
            return allFeatures.ToArray();
        }
    }
}