using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Core.Services;

public interface IEcgVisualizerService
{
    Image<Rgba32> CreateFullVisualisation(PreprocessingResult preprocessingResult);
    
}