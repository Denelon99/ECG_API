using ECG_API.Core.Services;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Rect = OpenCvSharp.Rect;
using Scalar = OpenCvSharp.Scalar;
using ECG_API.Infrastructure.Extensions;


namespace ECG_API.Infrastructure.ImageProcessing
{
    public class CalibrationPulseRemoverService(ILogger<CalibrationPulseRemoverService> logger)
        : ICalibrationPulseRemoverService
    {
                public Image<Rgba32> RemovePulse(Image<Rgba32> image)
        {
            try
            {
                using var cvImage = image.ToBitmap().ToMat();
                
                using var gray = new Mat();
                Cv2.CvtColor(cvImage, gray, ColorConversionCodes.BGR2GRAY);

                using var binary = new Mat();
                Cv2.Threshold(gray, binary, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
                
                using var projection = new Mat();
                Cv2.Reduce(binary, projection, ReduceDimension.Row, ReduceTypes.Sum, MatType.CV_32S);

                var gainLineCenters = FindGainLineCenters(projection, image.Width);

                if (gainLineCenters.Count > 0)
                {
                    logger.LogInformation("Знайдено {Count} ліній посилення. Видалення...", gainLineCenters.Count);
                    int removalWidth = 10; 

                    foreach (var centerX in gainLineCenters)
                    {
                        int startX = Math.Max(0, centerX - (removalWidth / 2));
                        var removalRect = new Rect(startX, 0, removalWidth, image.Height);
                        cvImage.Rectangle(removalRect, Scalar.White, -1);
                    }
                }
                else
                {
                    logger.LogWarning("Лінії посилення не знайдено.");
                }

                return cvImage.ToBitmap().ToImageSharp();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Помилка під час видалення ліній посилення.");
                return image.Clone();
            }
        }

        private List<int> FindGainLineCenters(Mat projection, int imageWidth)
        {
            var projectionValues = new List<double>();
            for (int x = 0; x < imageWidth; x++)
            {
                projectionValues.Add(projection.At<int>(0, x));
            }
            
            var sortedValues = new List<double>(projectionValues);
            sortedValues.Sort();
            
            int percentileIndex = (int)(sortedValues.Count * 0.95);
            double backgroundLevel = sortedValues[percentileIndex];
            double gainLineThreshold = backgroundLevel * 4.5;
            
            logger.LogInformation("Аналіз проекції для ліній посилення: Рівень фону={bg}, Поріг={threshold}",
                backgroundLevel, gainLineThreshold);

            var peakCenters = new List<int>();
            bool inPeak = false;
            int peakStart = 0;

            for (int x = 0; x < imageWidth; x++)
            {
                if (projectionValues[x] > gainLineThreshold && !inPeak)
                {
                    inPeak = true;
                    peakStart = x;
                }
                else if (projectionValues[x] < gainLineThreshold && inPeak)
                {
                    inPeak = false;
                    int peakEnd = x - 1;
                    int peakCenter = peakStart + (peakEnd - peakStart) / 2;
                    peakCenters.Add(peakCenter);
                }
            }

            return peakCenters;
        }
    }
}