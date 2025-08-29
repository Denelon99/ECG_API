using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Core.Services;

public interface IEcgBaselineService
{
    int FindBaseline(Image<Rgba32> ecgStrip);
}