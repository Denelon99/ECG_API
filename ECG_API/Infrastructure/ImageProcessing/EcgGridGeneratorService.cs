using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ECG_API.Infrastructure.ImageProcessing;

public class EcgGridGeneratorService(ILogger<EcgGridGeneratorService> logger) : IEcgGridGeneratorService
{
    private static readonly Color SmallLineColor = Color.FromRgb(255, 224, 224); // Світло-рожевий/червоний
    private static readonly Color LargeLineColor = Color.FromRgb(255, 180, 180); // Більш насичений
    private const float SmallLineThickness = 1f;
    private const float LargeLineThickness = 2f;
    public Image<Rgba32> GenerateGrid(int width, int height, float dotsPerMillimeter)
    {
        logger.LogInformation("Генерація сітки {Width}x{Height} з DPM={Dpm}", width, height, dotsPerMillimeter);

        // Створюємо біле полотно
        var gridImage = new Image<Rgba32>(width, height, Color.White);

        float smallStepPx = dotsPerMillimeter;       // 1 мм
        float largeStepPx = dotsPerMillimeter * 5;   // 5 мм
            
        gridImage.Mutate(ctx => 
        {
            // Малюємо горизонтальні лінії
            for (float y = 0; y < height; y += smallStepPx)
            {
                // Кожні 5 ліній робимо товстішою
                bool isLargeLine = (Math.Abs(y % largeStepPx) < smallStepPx / 2);
                ctx.DrawLine(
                    isLargeLine ? LargeLineColor : SmallLineColor,
                    isLargeLine ? LargeLineThickness : SmallLineThickness,
                    new PointF(0, y), new PointF(width, y)
                );
            }

            // Малюємо вертикальні лінії
            for (float x = 0; x < width; x += smallStepPx)
            {
                bool isLargeLine = (Math.Abs(x % largeStepPx) < smallStepPx / 2);
                ctx.DrawLine(
                    isLargeLine ? LargeLineColor : SmallLineColor,
                    isLargeLine ? LargeLineThickness : SmallLineThickness,
                    new PointF(x, 0), new PointF(x, height)
                );
            }
        });

        return gridImage;
    }
}