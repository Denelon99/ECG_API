using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
namespace ECG_API.Core.Services;

public interface IEcgArtifactRemoverService
{
    /// <summary>
    /// Видаляє з зображення текстові артефакти та калібрувальний імпульс.
    /// </summary>
    /// <param name="cleanSignal">Зображення з чистим, з'єднаним сигналом.</param>
    /// <returns>Зображення, що містить тільки чистий ЕКГ-сигнал.</returns>
    Image<Rgba32> RemoveArtifacts(Image<Rgba32> cleanSignal);
}