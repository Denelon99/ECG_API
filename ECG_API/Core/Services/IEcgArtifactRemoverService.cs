using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
namespace ECG_API.Core.Services;

public interface IEcgArtifactRemoverService
{
    Image<Rgba32> RemoveArtifacts(Image<Rgba32> cleanSignal);
}