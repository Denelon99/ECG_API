using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Core.Services;

public class GainLineDetectionResult
{
    public List<int> Centers { get; set; } = new();
    public Mat? Projection { get; set; }
    public double Threshold { get; set; }
}

public interface IGainLineDetectorService
{
    GainLineDetectionResult DetectGainLines(Image<Rgba32> ecgImage);
}