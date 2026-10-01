using ECG_API.Core.Services;
using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ECG_API.Infrastructure.Extensions;
using Rect = OpenCvSharp.Rect;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class EcgArtifactRemoverService(ILogger<EcgArtifactRemoverService> logger) : IEcgArtifactRemoverService
    {
        public Image<Rgba32> RemoveArtifacts(Image<Rgba32> cleanSignal)
        {
            try
            {
                using var cvImage = cleanSignal.ToMat();
                using var gray = new Mat();
                Cv2.CvtColor(cvImage, gray, ColorConversionCodes.BGR2GRAY);

                using var binary = new Mat();
                Cv2.Threshold(gray, binary, 254, 255, ThresholdTypes.BinaryInv);

                int pulseZoneWidth = 65; 
                var pulseZoneRect = new Rect(0, 0, pulseZoneWidth, binary.Height);
                binary.Rectangle(pulseZoneRect, Scalar.Black, -1);
                
                using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
                using var erodedImage = new Mat();
                Cv2.Erode(binary, erodedImage, kernel, iterations: 1);


                Cv2.FindContours(erodedImage.Clone(), out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                var contoursToRemove = contours.Where(c => Cv2.ContourArea(c) < 0.5).ToList();
                if (contoursToRemove.Count > 0)
                {
                    Cv2.DrawContours(erodedImage, contoursToRemove, -1, Scalar.Black, -1);
                }
                
                using var finalBinary = new Mat();
                Cv2.Dilate(erodedImage, finalBinary, kernel, iterations: 1);
                

                using var finalMat = new Mat(cvImage.Size(), MatType.CV_8UC3, Scalar.White);
                finalMat.SetTo(Scalar.Black, finalBinary);
                

                return finalMat.ToImageSharp();
            }
            catch (Exception ex)
            {
                return cleanSignal.Clone();
            }
        }
    }
}