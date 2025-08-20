using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Infrastructure.ImageProcessing;

public class EcgSignalIsolationService(ILogger<EcgSignalIsolationService> logger) : IEcgSignalIsolationService
{
    // Поріг для визначення "темного" пікселя (сигналу).
    private const byte ColorThreshold = 50; // Треб дізнатись більш коректну межу, але з 50 залишається тільки сигнал ЕКГ

    public Image<Rgba32> IsolateSignal(Image<Rgba32> ecgStrip)
    {
        logger.LogInformation("Початок ізоляції сигналу на зображенні {Width}x{Height}.", ecgStrip.Width,
            ecgStrip.Height);

        var signalOnlyImage = new Image<Rgba32>(ecgStrip.Width, ecgStrip.Height);

        for (int y = 0; y < ecgStrip.Height; y++)
        {
            for (int x = 0; x < ecgStrip.Width; x++)
            {
                Rgba32 originalPixel = ecgStrip[x, y];

                if (originalPixel.R < ColorThreshold &&
                    originalPixel.G < ColorThreshold &&
                    originalPixel.B < ColorThreshold)
                {
                    signalOnlyImage[x, y] = Color.Black;
                }
            }
        }

        logger.LogInformation("Ізоляція сигналу завершена.");
        return signalOnlyImage;
    }
}