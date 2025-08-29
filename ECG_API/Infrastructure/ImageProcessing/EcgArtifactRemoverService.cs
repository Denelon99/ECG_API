using ECG_API.Core.Services;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Drawing;
using ECG_API.Infrastructure.Extensions;
using Rect = OpenCvSharp.Rect;
using ImageSharpImage = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;
using SystemBitmap = System.Drawing.Bitmap;

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class EcgArtifactRemoverService(ILogger<EcgArtifactRemoverService> logger) : IEcgArtifactRemoverService
    {
        public Image<Rgba32> RemoveArtifacts(Image<Rgba32> cleanSignal)
        {
            try
            {
                using var cvImage = cleanSignal.ToBitmap().ToMat();
                using var gray = new Mat();
                Cv2.CvtColor(cvImage, gray, ColorConversionCodes.BGR2GRAY);

                using var binary = new Mat();
                Cv2.Threshold(gray, binary, 254, 255, ThresholdTypes.BinaryInv);

                int pulseZoneWidth = 65; 
                var pulseZoneRect = new Rect(0, 0, pulseZoneWidth, binary.Height);
                binary.Rectangle(pulseZoneRect, Scalar.Black, -1);
                logger.LogInformation("Зону стартового імпульсу (X < {Width}) очищено.", pulseZoneWidth);
                
                using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
                using var erodedImage = new Mat();
                Cv2.Erode(binary, erodedImage, kernel, iterations: 1);

                logger.LogInformation("Зображення після ерозії (схуднення).");

                Cv2.FindContours(erodedImage.Clone(), out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                var contoursToRemove = contours.Where(c => Cv2.ContourArea(c) < 0.5).ToList();
                if (contoursToRemove.Count > 0)
                {
                    Cv2.DrawContours(erodedImage, contoursToRemove, -1, Scalar.Black, -1);
                    logger.LogInformation("Додатково видалено {Count} дрібних залишків.", contoursToRemove.Count);
                }
                
                using var finalBinary = new Mat();
                Cv2.Dilate(erodedImage, finalBinary, kernel, iterations: 1);
                
                logger.LogInformation("Товщину сигналу відновлено.");

                using var finalMat = new Mat(cvImage.Size(), MatType.CV_8UC3, Scalar.White);
                finalMat.SetTo(Scalar.Black, finalBinary);
                

                return finalMat.ToBitmap().ToImageSharp();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Помилка під час морфологічного очищення артефактів.");
                return cleanSignal.Clone();
            }
        }
    }
}