using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Core.Services;

public interface IEcgWorkingAreaService
{
        Rectangle FindWorkingArea(Image<Rgba32> image);
}