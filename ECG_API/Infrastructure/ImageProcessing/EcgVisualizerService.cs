using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ECG_API.Infrastructure.ImageProcessing;
public class EcgVisualizerService(ILogger<EcgVisualizerService> logger,
    IEcgBaselineService baselineService)  : IEcgVisualizerService
{
    private static readonly Color BaselineColor = Color.Green;
    private static readonly Color AreaAboveColor = Color.FromRgba(66, 135, 245, 128);
    private static readonly Color AreaBelowColor = Color.FromRgba(245, 66, 66, 128);
    
    public Image<Rgba32> CreateFullVisualisation(PreprocessingResult preprocessingResult)
        {
            if (preprocessingResult?.FullCleanImage == null || !preprocessingResult.Strips.Any())
            {
                logger.LogWarning("Невалідні дані для повної візуалізації.");
                return new Image<Rgba32>(1, 1);
            }

            var canvas = preprocessingResult.FullCleanImage.Clone();

            int currentY_offset = 0;
            foreach (var strip in preprocessingResult.Strips)
            {
                int baselineY_relative = baselineService.FindBaseline(strip);
                
                if (baselineY_relative != -1)
                {
                    int baselineY_absolute = currentY_offset + baselineY_relative;
                    
                    PaintAreasOnCanvas(canvas, strip, baselineY_relative, currentY_offset);
                    
                    canvas.Mutate(ctx => ctx.DrawLine(BaselineColor, 2, 
                        new PointF(0, baselineY_absolute), 
                        new PointF(canvas.Width, baselineY_absolute)));
                }
                
                currentY_offset += strip.Height;
            }

            return canvas;
        }
        
        private void PaintAreasOnCanvas(Image<Rgba32> canvas, Image<Rgba32> strip, int baselineY, int yOffset)
        {
            for (int x = 0; x < strip.Width; x++)
            {
                int signalY = FindSignalYInColumn(strip, x);
                if (signalY != -1)
                {
                    if (signalY < baselineY)
                    {
                        for (int y = signalY; y < baselineY; y++)
                        {
                            int absoluteY = yOffset + y;
                            if (absoluteY < canvas.Height)
                            {
                                canvas[x, absoluteY] = Blend(canvas[x, absoluteY], AreaAboveColor);
                            }
                        }
                    }
                    else
                    {
                        for (int y = baselineY; y <= signalY; y++)
                        {
                            int absoluteY = yOffset + y;
                            if (absoluteY < canvas.Height)
                            {
                                canvas[x, absoluteY] = Blend(canvas[x, absoluteY], AreaBelowColor);
                            }
                        }
                    }
                }
            }
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