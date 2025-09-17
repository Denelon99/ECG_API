using ECG_API.Core.Services;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using ECG_API.Infrastructure.Extensions;
using ImageSharpImage = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class GainLineDetectorService(ILogger<GainLineDetectorService> logger) : IGainLineDetectorService
    {
        public List<int> DetectGainLineCenters(ImageSharpImage ecgImage)
        {
            try
            {
                using var cvImage = ecgImage.ToBitmap().ToMat();
                using var gray = new Mat();
                Cv2.CvtColor(cvImage, gray, ColorConversionCodes.BGR2GRAY);

                using var binary = new Mat();
                Cv2.Threshold(gray, binary, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
                
                using var projection = new Mat();
                Cv2.Reduce(binary, projection, ReduceDimension.Row, ReduceTypes.Sum, MatType.CV_32S);
                
                return FindPeakCenters(projection, ecgImage.Width);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Помилка під час детекції ліній посилення.");
                return new List<int>();
            }
        }

        private List<int> FindPeakCenters(Mat projection, int imageWidth)
        {
            var projectionValues = new List<double>();
            for (int x = 0; x < imageWidth; x++)
            {
                projectionValues.Add(projection.At<int>(0, x));
            }
            
            var sortedValues = new List<double>(projectionValues);
            sortedValues.Sort();
            
            int percentileIndex = (int)(sortedValues.Count * 0.99);
            double backgroundLevel = sortedValues[percentileIndex];
            
            double gainLineThreshold = backgroundLevel * 1.5;
            
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
            
            logger.LogInformation("Детектовано {Count} центрів ліній посилення.", peakCenters.Count);
            return peakCenters;
        }
    }
}