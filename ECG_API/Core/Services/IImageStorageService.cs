using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Core.Services;

public interface IImageStorageService
{
    Task<string> SaveImageAsync(Image<Rgba32> image, string fileName, string? subfolder = null);
    Task<List<string>> SaveImageStripsAsync(List<Image<Rgba32>> images, string baseFileName, string? subfolder = null);
    void DeleteUserImage(int userId);
}
