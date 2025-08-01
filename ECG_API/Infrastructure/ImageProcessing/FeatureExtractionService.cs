using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class FeatureExtractionService(ILogger<FeatureExtractionService> logger) : IFeatureExtractionService
    {
        private static readonly Color AreaAboveColor = Color.FromRgb(66, 135, 245); // Синій
        private static readonly Color AreaBelowColor = Color.FromRgb(245, 66, 66);  // Червоний

        public async Task<(float[]? features, byte[]? finalImage)> ProcessEcgImageAsync(Stream imageStream)
        {
            try
            {
                using var originalImage = await Image.LoadAsync<Rgba32>(imageStream);

                // TODO: Реалізувати вашу логіку обрізки та вибору смуги
                using var mainStrip = originalImage.Clone();
                using var signalOnlyImage = IsolateSignal(mainStrip);

                // --- Крок 2: Виділення ознак ---
                int baselineY = FindBaseline(signalOnlyImage); // TODO: реалізувати
                if (baselineY == -1) return (null, null);
                
                var features = ExtractFeatures(signalOnlyImage, baselineY);
                logger.LogInformation("Виділено {FeatureCount} ознак.", features.Length);
                // --- Крок 3: Створення фінального зображення для візуалізації ---
                using var finalVisualisation = CreateFinalVisualisation(signalOnlyImage, baselineY);

                // --- Крок 4: Конвертація зображення в масив байтів ---
                using var memoryStream = new MemoryStream();
                await finalVisualisation.SaveAsPngAsync(memoryStream);
                byte[] imageBytes = memoryStream.ToArray();
                
                return (features, imageBytes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Помилка під час обробки зображення.");
                return (null, null);
            }
        }
            // --- Допоміжні методи ---
            private Image<Rgba32> IsolateSignal(Image<Rgba32> strip)
            {
                var newImage = new Image<Rgba32>(strip.Width, strip.Height);
                for (int y = 0; y < strip.Height; y++)
                {
                    for (int x = 0; x < strip.Width; x++)
                    {
                        Rgba32 originalPixel = strip[x, y];
                        if (originalPixel.R < 150 && originalPixel.G < 150 && originalPixel.B < 150)
                        {
                            newImage[x, y] = originalPixel;
                        }
                    }
                }
                return newImage;
            }
        
        private int FindBaseline(Image<Rgba32> signalImage) 
        { /* ... ваша логіка ... */ 
            // TODO Середня лінія має бут наближеня до середньої лінії ЕКГ
            return signalImage.Height / 2; 
        }

        private float[] ExtractFeatures(Image<Rgba32> signalImage, int baselineY)
        {
            const int numberOfFragments = 20;
            // ... ваша логіка підрахунку площ ...
            return new float[numberOfFragments * 2]; // Повертаємо заглушку
        }

        // НОВИЙ МЕТОД ДЛЯ СТВОРЕННЯ ФІНАЛЬНОГО ЗОБРАЖЕННЯ
        private Image<Rgba32> CreateFinalVisualisation(Image<Rgba32> signalImage, int baselineY)
        {
            // Створюємо нове біле зображення того ж розміру
            var canvas = new Image<Rgba32>(signalImage.Width, signalImage.Height, Color.White);

            // Малюємо на ньому наш очищений сигнал
            canvas.Mutate(ctx => ctx.DrawImage(signalImage, 1f));

            // Малюємо базову лінію
            canvas.Mutate(ctx => ctx.DrawLine(Color.Green, 2, new PointF(0, baselineY), new PointF(canvas.Width, baselineY)));

            // Замальовуємо площі
            for (int x = 0; x < canvas.Width; x++)
            {
                int signalY = FindSignalY(signalImage, x); // TODO: реалізувати цей метод
                if (signalY != -1)
                {
                    if (signalY < baselineY) // Площа над лінією
                    {
                        for (int y = signalY; y < baselineY; y++)
                        {
                            canvas[x, y] = AreaAboveColor;
                        }
                    }
                    else // Площа під лінією
                    {
                        for (int y = baselineY; y <= signalY; y++)
                        {
                            canvas[x, y] = AreaBelowColor;
                        }
                    }
                }
            }
            return canvas;
        }

        private int FindSignalY(Image<Rgba32> image, int x) 
        { 
            for (int y = 0; y < image.Height; y++)
            {
                // Перевіряємо, чи піксель не прозорий
                if (image[x, y].A > 0) return y;
            }
            return -1;
        }
    }
}