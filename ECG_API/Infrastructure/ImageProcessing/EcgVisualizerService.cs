using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ECG_API.Infrastructure.ImageProcessing;
public class EcgVisualizerService(ILogger<EcgVisualizerService> logger)  : IEcgVisualizerService
{
    private static readonly Color BaselineColor = Color.Green;
    private static readonly Color AreaAboveColor = Color.FromRgba(66, 135, 245, 128);
    private static readonly Color AreaBelowColor = Color.FromRgba(245, 66, 66, 128);
    
    public Image<Rgba32> CreateVisualisation(Image<Rgba32> ecgStrip, int baselineY)
    {
        if (ecgStrip == null || baselineY < 0)
        {
            logger.LogWarning("Невалідні дані для візуалізації.");
            return new Image<Rgba32>(1, 1);
        }
            
        var canvas = ecgStrip.Clone();

        for (int x = 0; x < canvas.Width; x++)
        {
            int signalY = FindSignalYInColumn(ecgStrip, x);
            if (signalY != -1)
            {
                if (signalY < baselineY) // Площа над лінією
                {
                    for (int y = signalY; y < baselineY; y++)
                    {
                        canvas[x, y] = Blend(canvas[x, y], AreaAboveColor);
                    }
                }
                else // Площа під лінією
                {
                    for (int y = baselineY; y <= signalY; y++)
                    {
                        canvas[x, y] = Blend(canvas[x, y], AreaBelowColor);
                    }
                }
            }
        }
            
        canvas.Mutate(ctx => 
            ctx.DrawLine(BaselineColor, 2, new PointF(0, baselineY), new PointF(canvas.Width, baselineY))
        );
            
        logger.LogInformation("Створено зображення для візуалізації.");
        return canvas;
    }
    
    private int FindSignalYInColumn(Image<Rgba32> image, int x)
    {
        var yCoords = new List<int>();
        for (int y = 0; y < image.Height; y++)
        {
            if (image[x, y].R == 0) yCoords.Add(y);
        }
        if (yCoords.Count == 0) return -1;
        return (int)yCoords.Average();
    }
        
    private Rgba32 Blend(Rgba32 background, Rgba32 foreground)
    {
        if (foreground.A == 0) return background;
        if (foreground.A == 255) return foreground;

        float alpha = foreground.A / 255.0f;
        float invAlpha = 1 - alpha;

        byte r = (byte)(foreground.R * alpha + background.R * invAlpha);
        byte g = (byte)(foreground.G * alpha + background.G * invAlpha);
        byte b = (byte)(foreground.B * alpha + background.B * invAlpha);

        return new Rgba32(r, g, b, 255);
    }
}