using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class EcgStripSplitterService(ILogger<EcgStripSplitterService> logger) : IEcgStripSplitterService
    {
        public List<Image<Rgba32>> SplitIntoStrips(Image<Rgba32> image, int numberOfStrips)
        {
            if (image == null || numberOfStrips <= 0)
            {
                logger.LogWarning("На вхід SplitIntoStrips передано невалідні дані.");
                return new List<Image<Rgba32>>();
            }

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
                int y = i * stripHeight;
                
                int height = (i == numberOfStrips - 1)
                    ? image.Height - y
                    : stripHeight;

                if (y + height > image.Height)
                {
                    height = image.Height - y;
                }

                if (height <= 0) continue;

                var rect = new Rectangle(0, y, image.Width, height);
                var strip = image.Clone(ctx => ctx.Crop(rect));
                result.Add(strip);
            }

            return result;
        }
    }
}