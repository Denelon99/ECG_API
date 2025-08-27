using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
namespace ECG_API.Core.Services;

public interface IEcgSignalConnectorService
{
    /// <summary>
    /// Виконує фінальне очищення ізольованого сигналу: видаляє шум, з'єднує розриви.
    /// </summary>
    Image<Rgba32> ConnectSignal(Image<Rgba32> isolatedSignal);
}