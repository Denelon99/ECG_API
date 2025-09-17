using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
    
namespace ECG_API.Core.Services;

public interface IFeatureCalculatorService
{
    float[] CalculateFeatures(Image<Rgba32> ecgStrip, int baselineY);
}