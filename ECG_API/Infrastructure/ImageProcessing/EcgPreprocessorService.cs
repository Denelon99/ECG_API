using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class EcgPreprocessorService(
        ILogger<EcgPreprocessorService> logger,
        IEcgWorkingAreaService workingAreaService,
        IEcgSignalIsolationService signalIsolationService,
        ICalibrationPulseRemoverService pulseRemoverService,
        IEcgArtifactRemoverService artifactRemoverService,
        IEcgSignalConnectorService signalConnectorService,
        IEcgStripSplitterService stripSplitterService) : IEcgPreprocessorService
    {
        public List<Image<Rgba32>> Preprocess(Image<Rgba32> originalImage)
        {
            logger.LogInformation("--- Початок конвеєра попередньої обробки ---");

            using var croppedImage = workingAreaService.FindAndCropWorkingArea(originalImage);
        
            if (croppedImage == null)
            {
                logger.LogError("Не вдалося отримати робочу область. Зупинка обробки.");
                return new List<Image<Rgba32>>();
            }            
            using var isolatedSignal = signalIsolationService.IsolateSignal(croppedImage);
            
            using var signalWithBackground = new Image<Rgba32>(isolatedSignal.Width, isolatedSignal.Height, Color.White);
            signalWithBackground.Mutate(ctx => ctx.DrawImage(isolatedSignal, 1f));
            
            using var imageWithoutPulse = pulseRemoverService.RemovePulse(signalWithBackground);
            
            using var artifactsRemoved = artifactRemoverService.RemoveArtifacts(imageWithoutPulse);
            
            using var cleanSignal = signalConnectorService.ConnectSignal(artifactsRemoved);
            
            var strips = stripSplitterService.SplitIntoStrips(cleanSignal, 4);
            
            logger.LogInformation("--- Попередня обробка завершена. Отримано {Count} смуг. ---", strips.Count);
            return strips;
        }
    }
}