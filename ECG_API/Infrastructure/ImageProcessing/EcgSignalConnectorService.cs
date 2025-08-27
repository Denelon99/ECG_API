using ECG_API.Core.Services;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Point = OpenCvSharp.Point;
using ImageSharpImage = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;
using SystemBitmap = System.Drawing.Bitmap;


namespace ECG_API.Infrastructure.ImageProcessing
{
    public class EcgSignalConnectorService(ILogger<EcgSignalConnectorService> logger) : IEcgSignalConnectorService
    {
        public Image<Rgba32> ConnectSignal (Image<Rgba32> signalWithGaps)
        {
            try
            {
                using var cvImage = ConvertImageSharpToBitmap(signalWithGaps).ToMat();
                using var gray = new Mat();
                Cv2.CvtColor(cvImage, gray, ColorConversionCodes.BGR2GRAY);
                using var binary = new Mat();
                Cv2.Threshold(gray, binary, 254, 255, ThresholdTypes.BinaryInv);

                // --- Крок 1: Знаходимо зони розривів ---
                using var projection = new Mat();
                Cv2.Reduce(binary, projection, ReduceDimension.Row, ReduceTypes.Sum, MatType.CV_32S);
                var gapZones = FindGapZones(projection, cvImage.Width);
                logger.LogInformation("Знайдено {Count} зон розривів для з'єднання.", gapZones.Count);

                // --- Крок 2: З'єднуємо розриви для кожної смуги окремо ---
                foreach (var (start, end) in gapZones)
                {
                    ConnectGapMultiLane(binary, start, end);
                }

                // --- Створюємо фінальне зображення ---
                using var finalMat = new Mat(cvImage.Size(), MatType.CV_8UC3, Scalar.White);
                finalMat.SetTo(Scalar.Black, binary);

                return ConvertBitmapToImageSharp(finalMat.ToBitmap());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Помилка під час з'єднання розривів сигналу.");
                return signalWithGaps.Clone();
            }
        }

        private List<(int start, int end)> FindGapZones(Mat projection, int imageWidth)
        {
            var gapZones = new List<(int start, int end)>();
            const int gapThreshold = 2; 

            bool inGap = false;
            int gapStart = 0;

            for (int x = 0; x < imageWidth; x++)
            {
                var value = projection.At<int>(0, x);
                if (value <= gapThreshold * 255 && !inGap)
                {
                    inGap = true;
                    gapStart = x;
                }
                else if (value > gapThreshold * 255 && inGap)
                {
                    inGap = false;
                    int gapEnd = x - 1;
                    if (gapEnd - gapStart > 5 && gapEnd - gapStart < 50)
                    {
                        gapZones.Add((gapStart, gapEnd));
                    }
                }
            }
            return gapZones;
        }
        
        private void ConnectGapMultiLane(Mat binaryImage, int startX, int endX)
        {
            // 1. Знаходимо всі окремі шматки сигналу зліва та справа від розриву
            var leftEndpoints = FindSignalEndpointsInColumn(binaryImage, startX - 1);
            var rightEndpoints = FindSignalEndpointsInColumn(binaryImage, endX + 1);
            
            if (leftEndpoints.Count == 0 || rightEndpoints.Count == 0) return;

            logger.LogDebug("Для розриву X=[{start}..{end}] знайдено {L} лівих та {R} правих кінцевих точок.",
                startX, endX, leftEndpoints.Count, rightEndpoints.Count);

            // 2. Для кожної лівої точки шукаємо найближчу праву точку на тій же висоті
            foreach (var leftPoint in leftEndpoints)
            {
                // Знаходимо праву точку, яка є найближчою по осі Y
                var bestMatch = rightEndpoints
                    .OrderBy(rightPoint => Math.Abs(rightPoint.Y - leftPoint.Y))
                    .FirstOrDefault();
                
                // З'єднуємо, тільки якщо вони на одній "лінії" (різниця по Y не велика)
                if (bestMatch != null && Math.Abs(bestMatch.Y - leftPoint.Y) < 100) // Поріг 20px
                {
                    Cv2.Line(binaryImage, leftPoint, bestMatch, Scalar.White, 2);
                    // Видаляємо знайдену праву точку, щоб не з'єднати її з іншою лівою
                    rightEndpoints.Remove(bestMatch); 
                }
            }
        }
        
        private List<Point> FindSignalEndpointsInColumn(Mat binaryImage, int x)
        {
            var endpoints = new List<Point>();
            if (x < 0 || x >= binaryImage.Width) return endpoints;

            // Вирізаємо вузьку смугу для аналізу
            int searchWidth = 10;
            int roiX = Math.Max(0, x - searchWidth / 2);
            var roiRect = new Rect(roiX, 0, Math.Min(searchWidth, binaryImage.Width - roiX), binaryImage.Height);
            
            using var roi = new Mat(binaryImage, roiRect);
            
            // Шукаємо контури ТІЛЬКИ в цій вузькій смузі
            Cv2.FindContours(roi, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                var moments = Cv2.Moments(contour);
                if (moments.M00 == 0) continue;

                int cX = (int)(moments.M10 / moments.M00);
                int cY = (int)(moments.M01 / moments.M00);
                
                // Повертаємо абсолютні координати
                endpoints.Add(new Point(roiRect.X + cX, roiRect.Y + cY));
            }
            return endpoints;
        }
        
        // --- Методи-конвертери (залишаються без змін) ---
        private SystemBitmap ConvertImageSharpToBitmap(ImageSharpImage image)
        {
            using var memoryStream = new MemoryStream();
            image.SaveAsBmp(memoryStream);
            memoryStream.Position = 0;
            return new SystemBitmap(memoryStream);
        }

        private ImageSharpImage ConvertBitmapToImageSharp(SystemBitmap bitmap)
        {
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Position = 0;
            return ImageSharpImage.Load<Rgba32>(memoryStream);
        }
    }
}