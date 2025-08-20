using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Core.Services;

public interface IImageStorageService
{
    Task<string> SaveImageAsync(Image<Rgba32> image, string fileName, string? subfolder = null);
}