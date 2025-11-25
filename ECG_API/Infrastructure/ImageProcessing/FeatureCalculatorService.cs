using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Infrastructure.ImageProcessing;

public class FeatureCalculatorService(ILogger<FeatureCalculatorService> logger) : IFeatureCalculatorService
{
    private const int NumberOfFragments = 20;

    public float[] CalculateFeatures(Image<Rgba32> ecgStrip, int baselineY)
    {
        var features = new List<float>();
        if (ecgStrip == null || baselineY < 0)
        {
            logger.LogWarning("Невалідні дані для розрахунку ознак: зображення або базова лінія відсутні.");
            return new float[NumberOfFragments * 2]; 
        }

        int fragmentWidth = ecgStrip.Width / NumberOfFragments;
        if (fragmentWidth == 0)
        {
            logger.LogWarning("Ширина смуги занадто мала для розбиття на {NumFragments} фрагментів.",
                NumberOfFragments);
            return new float[NumberOfFragments * 2];
        }

        for (int i = 0; i < NumberOfFragments; i++)
        {
            int startX = i * fragmentWidth;
            int endX = (i == NumberOfFragments - 1) ? ecgStrip.Width : startX + fragmentWidth;

            float areaAbove = 0;
            float areaBelow = 0;

            for (int x = startX; x < endX; x++)
            {
                int signalY = FindSignalYInColumn(ecgStrip, x);
                if (signalY != -1)
                {
                    areaAbove += Math.Max(0, baselineY - signalY);
                    areaBelow += Math.Max(0, signalY - baselineY);
                }
            }

            features.Add(areaAbove);
            features.Add(areaBelow);
        }

        return features.ToArray();
    }

    private int FindSignalYInColumn(Image<Rgba32> image, int x)
    {
        var yCoords = new List<int>();
        for (int y = 0; y < image.Height; y++)
        {
            if (image[x, y].R == 0 && image[x, y].G == 0 && image[x, y].B == 0)
            {
                yCoords.Add(y);
            }
        }

        if (yCoords.Count == 0) return -1;
        return (int)yCoords.Average();
    }
}