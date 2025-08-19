using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace ECG_API.Core.Services;

public interface IEcgStripSplitterService
{
    List<Image<Rgba32>>  SplitIntoStrips(Image<Rgba32> image, int numberOfStrips);
}