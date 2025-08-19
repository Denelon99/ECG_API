using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class EcgStripSplitterService(ILogger<EcgStripSplitterService> logger) : IEcgStripSplitterService
    {
        // Метод тепер приймає numberOfStrips, як визначено в інтерфейсі
        public List<Image<Rgba32>> SplitIntoStrips(Image<Rgba32> image, int numberOfStrips)
        {
            if (image == null || numberOfStrips <= 0)
            {
                logger.LogWarning("На вхід SplitIntoStrips передано невалідні дані.");
                return new List<Image<Rgba32>>();
            }

            // Розраховуємо висоту однієї смуги
            int stripHeight = image.Height / numberOfStrips;
            if (stripHeight == 0)
            {
                logger.LogError(
                    "Висота зображення ({ImageHeight}) занадто мала для розділення на {NumberOfStrips} смуг.",
                    image.Height, numberOfStrips);
                return new List<Image<Rgba32>>();
            }

            var result = new List<Image<Rgba32>>();

            for (int i = 0; i < numberOfStrips; i++)
            {
                // Початкова Y координата для поточної смуги
                int y = i * stripHeight;

                // --- ЛОГІКА ДЛЯ ОСТАННЬОЇ СМУГИ ---
                // Якщо це остання смуга, беремо всю залишкову висоту,
                // щоб уникнути помилок округлення.
                int height = (i == numberOfStrips - 1)
                    ? image.Height - y
                    : stripHeight;

                // Переконуємось, що висота не виходить за межі
                if (y + height > image.Height)
                {
                    height = image.Height - y;
                }

                // Якщо висота стала нульовою або від'ємною, зупиняємось
                if (height <= 0) continue;

                var rect = new Rectangle(0, y, image.Width, height);
                var strip = image.Clone(ctx => ctx.Crop(rect));
                result.Add(strip);
            }

            logger.LogInformation("Зображення було вдало розділено на {StripsCount} горизонтальних смуг.",
                result.Count);
            return result;
        }
    }
}