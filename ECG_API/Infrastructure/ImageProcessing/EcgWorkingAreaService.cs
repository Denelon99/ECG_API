using ECG_API.Core.Services;
using ECG_API.Infrastructure.Extensions;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Point = OpenCvSharp.Point;

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class EcgWorkingAreaService(ILogger<EcgWorkingAreaService> logger) : IEcgWorkingAreaService
    {
        public Image<Rgba32>? FindAndCropWorkingArea(Image<Rgba32> originalImage)
        {
            try
            {
                using var cvImage = originalImage.ToBitmap().ToMat();
                using var gray = new Mat();
                Cv2.CvtColor(cvImage, gray, ColorConversionCodes.BGR2GRAY);
                
                var signalRect = DetectSignalArea(gray);
                
                if (signalRect.Width <= 0 || signalRect.Height <= 0)
                {
                    logger.LogWarning("Робочу область не знайдено, повертаємо повне зображення.");
                    return originalImage.Clone();
                }
                
                var cropArea = new Rectangle(signalRect.X, signalRect.Y, signalRect.Width, signalRect.Height);
                
                return CropImageSafely(originalImage, cropArea);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Помилка при пошуку та обрізці робочої області ЕКГ");
                return null;
            }
        }
        private Image<Rgba32> CropImageSafely(Image<Rgba32> image, Rectangle cropArea)
        {
            var safeX = Math.Max(0, Math.Min(cropArea.X, image.Width - 1));
            var safeY = Math.Max(0, Math.Min(cropArea.Y, image.Height - 1));
            var safeWidth = Math.Max(1, Math.Min(cropArea.Width, image.Width - safeX));
            var safeHeight = Math.Max(1, Math.Min(cropArea.Height, image.Height - safeY));

            var safeCropArea = new Rectangle(safeX, safeY, safeWidth, safeHeight);

            var croppedImage = image.Clone();
            
            croppedImage.Mutate(ctx => ctx.Crop(safeCropArea));
            return croppedImage;
        }

        private Rect DetectSignalArea(Mat grayImage)
        {
            using var binary = new Mat();
            Cv2.Threshold(grayImage, binary, 200, 255, ThresholdTypes.BinaryInv);

            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(15, 5));
            using var connected = new Mat();
            Cv2.MorphologyEx(binary, connected, MorphTypes.Close, kernel);

            Cv2.FindContours(connected, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
    
            if (contours.Length == 0)
            {
                logger.LogWarning("Не знайдено жодних контурів сигналу на зображенні.");
                return new OpenCvSharp.Rect();
            }
    
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
    
            OpenCvSharp.Rect boundingRect = Cv2.BoundingRect(largestContour);

            int margin = 0;
            boundingRect.X = Math.Max(0, boundingRect.X - margin);
            boundingRect.Y = Math.Max(0, boundingRect.Y - margin);
            int newWidth = Math.Min(grayImage.Width - boundingRect.X, boundingRect.Width + margin * 2);
            int newHeight = Math.Min(grayImage.Height - boundingRect.Y, boundingRect.Height + margin * 2);
    
            boundingRect.Width = newWidth;
            boundingRect.Height = newHeight;
    
            return boundingRect;
        }
    }
}