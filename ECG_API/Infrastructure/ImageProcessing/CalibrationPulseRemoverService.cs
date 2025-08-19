using ECG_API.Core.Services;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ImageSharpImage = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;
using SystemBitmap = System.Drawing.Bitmap;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;
using Size = OpenCvSharp.Size; 

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class CalibrationPulseRemoverService(ILogger<CalibrationPulseRemoverService> logger) : ICalibrationPulseRemoverService
    {
        public Image<Rgba32> RemovePulse(Image<Rgba32> image)
        {
            try
            {
                using var cvImage = ConvertImageSharpToBitmap(image).ToMat();
                using var diagnosticImage = cvImage.Clone(); // Створюємо копію для малювання
                
                using var gray = new Mat();
                Cv2.CvtColor(cvImage, gray, ColorConversionCodes.BGR2GRAY);
                
                using var binary = new Mat();
                // Поріг зменшено до 120
                Cv2.Threshold(gray, binary, 50, 255, ThresholdTypes.BinaryInv);
                
                using var verticalKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(1, 15));
                using var verticalStructures = new Mat();
                
                Cv2.Dilate(binary, verticalStructures, verticalKernel, iterations: 2);
                Cv2.Erode(verticalStructures, verticalStructures, verticalKernel, iterations: 1);
                
                Cv2.FindContours(verticalStructures, out Point[][] contours, out _, 
                    RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                
                logger.LogInformation("Знайдено {Count} потенційних вертикальних структур.", contours.Length);
                
                // --- ВІДЛАДКА: Малюємо ВСІ знайдені контури зеленим ---
                Cv2.DrawContours(diagnosticImage, contours, -1, Scalar.Green, 2);
                
                Rect pulseBoundingBox = FindPulseContour(contours, image.Height, image.Width);
                
                if (pulseBoundingBox.Width > 0 && pulseBoundingBox.Height > 0)
                {
                    logger.LogInformation("Видалення калібрувального імпульсу в області: {Rect}", pulseBoundingBox);
                    diagnosticImage.Rectangle(pulseBoundingBox, Scalar.Red, -1); 
                }
                else
                {
                    logger.LogWarning("Калібрувальний імпульс не знайдено. Зображення не змінено.");
                }
                
                // --- КОНВЕРТАЦІЯ НАЗАД ---
                using var resultBitmap = BitmapConverter.ToBitmap(diagnosticImage);
                return ConvertBitmapToImageSharp(resultBitmap);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Помилка під час видалення калібрувального імпульсу.");
                return image.Clone();
            }
        }
        
        // --- Допоміжні методи для конвертації ---
        private SystemBitmap ConvertImageSharpToBitmap(ImageSharpImage image)
        {
            using var memoryStream = new MemoryStream();
            // Зберігаємо зображення ImageSharp в потік у форматі Bmp
            image.SaveAsBmp(memoryStream);
            memoryStream.Position = 0;
            // Створюємо Bitmap з потоку
            return new SystemBitmap(memoryStream);
        }

        private ImageSharpImage ConvertBitmapToImageSharp(SystemBitmap bitmap)
        {
            using var memoryStream = new MemoryStream();
            // Зберігаємо Bitmap в потік
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Position = 0;
            // Завантажуємо зображення ImageSharp з потоку
            return ImageSharpImage.Load<Rgba32>(memoryStream);
        }

        // Ваш метод FindPulseContour залишається без змін
        private Rect FindPulseContour(Point[][] contours, int imageHeight, int imageWidth)
        {
            Rect bestCandidate = new Rect();
            double maxScore = 0;

            foreach (var contour in contours)
            {
                var box = Cv2.BoundingRect(contour);

                // Критерій 1: Висота
                double heightRatio = (double)box.Height / imageHeight;
                if (heightRatio < 0.5) continue;

                // Критерій 2: Ширина
                if (box.Width > 30) continue;
        
                // Критерій 3: Позиція (в лівих 15% зображення)
                if (box.Left > imageWidth * 0.15) continue;
        
                // Рахуємо оцінку
                double score = heightRatio - ((double)box.Left / imageWidth); // Бонус за близькість до лівого краю
        
                if (score > maxScore)
                {
                    maxScore = score;
                    bestCandidate = box;
                }
            }

            return bestCandidate;
        }
    }
}