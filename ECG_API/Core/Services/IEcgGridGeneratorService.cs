using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Core.Services;

public interface IEcgGridGeneratorService
{
    Image<Rgba32> GenerateGrid(int width, int height, float dotsPerMillimeter);
}