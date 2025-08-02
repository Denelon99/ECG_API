using ECG_API.Core.Services;
using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Point = OpenCvSharp.Point;

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class EcgWorkingAreaService(ILogger<EcgWorkingAreaService> logger) : IEcgWorkingAreaService
    {
        public Rectangle FindWorkingArea(Image<Rgba32> image)
        {
            try
            {
                // Конвертуємо ImageSharp в OpenCV Mat
                using var cvImage = ConvertToOpenCvMat(image);
                
                // Конвертуємо в grayscale
                using var gray = new Mat();
                Cv2.CvtColor(cvImage, gray, ColorConversionCodes.BGR2GRAY);
                
                // Якщо червоної рамки немає, шукаємо область з сигналом
                var signalRect = DetectSignalArea(gray);
                
                logger?.LogInformation($"Знайдено область сигналу: {signalRect}");
                return new Rectangle(signalRect.X, signalRect.Y, signalRect.Width, signalRect.Height);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Помилка при пошуку робочої області ЕКГ");
                throw;
            }
        }

        private Mat ConvertToOpenCvMat(Image<Rgba32> image)
        {
            var mat = new Mat(image.Height, image.Width, MatType.CV_8UC3);
            
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < image.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    var matIndexer = mat.GetGenericIndexer<Vec3b>();
                    
                    for (int x = 0; x < image.Width; x++)
                    {
                        var pixel = row[x];
                        // OpenCV використовує BGR, а не RGB
                        matIndexer[y, x] = new Vec3b(pixel.B, pixel.G, pixel.R);
                    }
                }
            });
            
            return mat;
        }

        private Rect DetectSignalArea(Mat grayImage)
        {
            // Крок 1: Бінаризація для виділення темних ліній (сигнал ЕКГ та сітка)
            using var binary = new Mat();
            Cv2.Threshold(grayImage, binary, 200, 255, ThresholdTypes.BinaryInv);

            // Крок 2: Морфологічні операції для з'єднання розривних частин сигналу
            // Використовуємо трохи більший kernel, щоб надійно з'єднати все в один великий "блоб"
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(15, 5));
            using var connected = new Mat();
            Cv2.MorphologyEx(binary, connected, MorphTypes.Close, kernel);

            // Крок 3: Пошук контурів на об'єднаному зображенні
            Cv2.FindContours(connected, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
    
            // Перевірка, чи знайдено хоч якісь контури
            if (contours.Length == 0)
            {
                logger.LogWarning("Не знайдено жодних контурів сигналу на зображенні.");
                return new OpenCvSharp.Rect(); // Повертаємо пустий прямокутник
            }
    
            // Крок 4: Знаходимо контур з найбільшою площею
            double maxArea = 0;
            Point[] largestContour = contours[0];

            foreach (var contour in contours)
            {
                double area = Cv2.ContourArea(contour);
                if (area > maxArea)
                {
                    maxArea = area;
                    largestContour = contour;
                }
            }
    
            logger.LogInformation("Знайдено найбільший контур з площею: {MaxArea}", maxArea);
    
            // Крок 5: Повертаємо обмежуючий прямокутник для найбільшого контуру
            OpenCvSharp.Rect boundingRect = Cv2.BoundingRect(largestContour);

            // Крок 6 (опціонально): Додаємо невеликі відступи (margins), щоб нічого не обрізати
            int margin = -7; // 10 пікселів з кожного боку
            boundingRect.X = Math.Max(0, boundingRect.X - margin);
            boundingRect.Y = Math.Max(0, boundingRect.Y - margin);
            // Розраховуємо нову ширину та висоту, враховуючи відступи з обох боків
            int newWidth = Math.Min(grayImage.Width - boundingRect.X, boundingRect.Width + margin * 2);
            int newHeight = Math.Min(grayImage.Height - boundingRect.Y, boundingRect.Height + margin * 2);
    
            boundingRect.Width = newWidth;
            boundingRect.Height = newHeight;
    
            return boundingRect;
        }
    }
}