using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Core.Services;
public interface IFeatureExtractionService
{
    float[] ExtractFeatures(List<Image<Rgba32>> ecgStrips);
}