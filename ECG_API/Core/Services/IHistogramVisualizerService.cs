using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Core.Services
{
    public interface IHistogramVisualizerService
    {
        Image<Rgba32> CreateVerticalProjectionHistogram(Image<Rgba32> ecgImage);
    }
}