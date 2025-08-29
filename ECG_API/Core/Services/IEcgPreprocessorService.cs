using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Core.Services;

public interface IEcgPreprocessorService
{
    List<Image<Rgba32>> Preprocess(Image<Rgba32> originalImage);
}