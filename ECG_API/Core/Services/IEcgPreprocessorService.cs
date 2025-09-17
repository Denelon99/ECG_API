using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Core.Services;
public class PreprocessingResult
{
    public Image<Rgba32> FullCleanImage { get; set; }
    public List<Image<Rgba32>> Strips { get; set; }
}
public interface IEcgPreprocessorService
{
    Task<PreprocessingResult> PreprocessAsync(Image<Rgba32> originalImage);
}
