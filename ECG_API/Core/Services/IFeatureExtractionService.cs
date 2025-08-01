namespace ECG_API.Core.Services;

public interface IFeatureExtractionService
{
    Task<(float[]? features, byte[]? finalImage)> ProcessEcgImageAsync(Stream imageStream);
}