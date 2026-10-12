using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ECG_API.Infrastructure.ImageProcessing
{
    public class EcgPreprocessorService(
        IEcgWorkingAreaService workingAreaService,
        IEcgSignalIsolationService signalIsolationService,
        ICalibrationPulseRemoverService pulseRemoverService,
        IEcgArtifactRemoverService artifactRemoverService,
        IEcgSignalConnectorService signalConnectorService,
        IEcgStripSplitterService stripSplitterService) : IEcgPreprocessorService
    {
        public Task<PreprocessingResult> PreprocessAsync(Image<Rgba32> originalImage)
        {
            try
            {
                using var croppedImage = workingAreaService.FindAndCropWorkingArea(originalImage) 
                                         ?? throw new InvalidOperationException("Не вдалося обрізати робочу область.");

                using var isolatedSignal = signalIsolationService.IsolateSignal(croppedImage);
            
                using var signalWithBackground = new Image<Rgba32>(isolatedSignal.Width, isolatedSignal.Height, Color.White);
                signalWithBackground.Mutate(ctx => ctx.DrawImage(isolatedSignal, 1f));
            
                using var imageWithoutGainLines = pulseRemoverService.RemovePulse(signalWithBackground);
                using var artifactsRemoved = artifactRemoverService.RemoveArtifacts(imageWithoutGainLines);
                using var finalCleanSignal = signalConnectorService.ConnectSignal(artifactsRemoved);
            
                var strips = stripSplitterService.SplitIntoStrips(finalCleanSignal, 4);

                return Task.FromResult(new PreprocessingResult
                {
                    FullCleanImage = finalCleanSignal.Clone(),
                    Strips = strips
                });
            }
            catch (Exception exception)
            {
                return Task.FromException<PreprocessingResult>(exception);
            }
        }
    }
}
