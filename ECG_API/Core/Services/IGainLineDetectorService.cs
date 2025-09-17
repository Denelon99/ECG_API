namespace ECG_API.Core.Services;

public interface IGainLineDetectorService
{
    List<int> DetectGainLineCenters(SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> ecgImage);
}