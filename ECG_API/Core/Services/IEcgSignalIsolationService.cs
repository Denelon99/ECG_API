using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Core.Services;

public interface IEcgSignalIsolationService
{
    Image<Rgba32> IsolateSignal(Image<Rgba32> ecgStrip);
}