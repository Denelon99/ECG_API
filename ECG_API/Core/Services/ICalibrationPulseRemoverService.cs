using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
namespace ECG_API.Core.Services;

public interface ICalibrationPulseRemoverService
{
    Image<Rgba32> RemovePulse(Image<Rgba32> image);
}