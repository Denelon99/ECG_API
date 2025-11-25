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
        IImageStorageService imageStorageService,
        IHistogramVisualizerService histogramVisualizerService,
        IEcgStripSplitterService stripSplitterService) : IEcgPreprocessorService
    {
        public async Task<PreprocessingResult> PreprocessAsync(Image<Rgba32> originalImage)
        {

            using var croppedImage = workingAreaService.FindAndCropWorkingArea(originalImage) 
                                     ?? throw new InvalidOperationException("Не вдалося обрізати робочу область.");

            using var isolatedSignal = signalIsolationService.IsolateSignal(croppedImage);
            
            using var signalWithBackground = new Image<Rgba32>(isolatedSignal.Width, isolatedSignal.Height, Color.White);
            signalWithBackground.Mutate(ctx => ctx.DrawImage(isolatedSignal, 1f));
            
            using var histogramImage = histogramVisualizerService.CreateVerticalProjectionHistogram(signalWithBackground);
            
            using var imageWithoutGainLines = pulseRemoverService.RemovePulse(signalWithBackground);
            using var artifactsRemoved = artifactRemoverService.RemoveArtifacts(imageWithoutGainLines);
            using var finalCleanSignal = signalConnectorService.ConnectSignal(artifactsRemoved);
            
            var strips = stripSplitterService.SplitIntoStrips(finalCleanSignal, 4);

            if (strips.Any())
            {
                var baseFileName = $"ecg_{DateTime.Now:yyyyMMdd_HHmmssfff}";
                await imageStorageService.SaveImageStripsAsync(strips, baseFileName, "strips");
            }

            return new PreprocessingResult
            {
                FullCleanImage = finalCleanSignal.Clone(),
                Strips = strips
            };
        }
    }
}