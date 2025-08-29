using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Core.Services;

public interface IEcgWorkingAreaService
{
        Image<Rgba32>? FindAndCropWorkingArea(Image<Rgba32> originalImage);
}