using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ECG_API.Core.Services;
using ECG_API.Core.MLModels;

namespace ECG_API.Controllers
{
    public class AnalyzeImageRequest
    {
        [Required]
        public required IFormFile ImageFile { get; set; }
    }
    
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController : ControllerBase
    {
        private readonly ILogger<AnalysisController> _logger;
        private readonly IEcgPreprocessorService _preprocessorService;
        private readonly IFeatureExtractionService _featureExtractionService;
        private readonly IPredictionService _predictionService;
        private readonly IEcgVisualizerService _visualizerService;
        private readonly IEcgBaselineService _baselineService;
        private readonly IImageStorageService _imageStorageService;

        public AnalysisController(
            ILogger<AnalysisController> logger, 
            IEcgPreprocessorService preprocessorService,
            IFeatureExtractionService featureExtractionService,
            IPredictionService predictionService,
            IEcgVisualizerService visualizerService,
            IEcgBaselineService baselineService,
            IImageStorageService imageStorageService)
        {
            _logger = logger;
            _preprocessorService = preprocessorService;
            _featureExtractionService = featureExtractionService;
            _predictionService = predictionService;
            _visualizerService = visualizerService;
            _baselineService = baselineService;
            _imageStorageService = imageStorageService;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeImage([FromForm] AnalyzeImageRequest request)
        {
            List<Image<Rgba32>>? cleanStrips = null;
            try
            {
                await using var imageStream = request.ImageFile.OpenReadStream();
                using var originalImage = await Image.LoadAsync<Rgba32>(imageStream);

                var preprocessingResult = await _preprocessorService.PreprocessAsync(originalImage);
                cleanStrips = preprocessingResult.Strips;

                using (preprocessingResult.FullCleanImage)
                {
                    if (!cleanStrips.Any())
                    {
                        return StatusCode(500, "Помилка: не отримано жодної смуги після попередньої обробки.");
                    }

                    await _imageStorageService.SaveImageAsync(preprocessingResult.FullCleanImage, 
                        $"full_clean_{DateTime.Now:yyyyMMdd_HHmmssfff}", "full_results");

                    var features = _featureExtractionService.ExtractFeatures(cleanStrips);

                    var modelInput = new ModelInput { Features = features };
                    var prediction = _predictionService.Predict(modelInput);
                    string predictedDiagnosis = prediction.PredictedDiagnosis;
                    _logger.LogInformation(">>> ПРОГНОЗ МОДЕЛІ: {Diagnosis} <<<", predictedDiagnosis);

                    byte[] visualizationBytes;
                    using (var firstStrip = cleanStrips[0].Clone())
                    {
                        var baseline = _baselineService.FindBaseline(firstStrip);
                        using var visualImage = _visualizerService.CreateVisualisation(firstStrip, baseline);
                        
                        await _imageStorageService.SaveImageAsync(visualImage, $"result_{DateTime.Now:yyyyMMdd_HHmmssfff}", "results");

                        using var ms = new MemoryStream();
                        await visualImage.SaveAsPngAsync(ms);
                        visualizationBytes = ms.ToArray();
                    }
                    
                    var featuresString = string.Join(";", features.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                    Response.Headers.Append("X-ECG-Predicted-Diagnosis", predictedDiagnosis);
                    Response.Headers.Append("X-ECG-Feature-Count", features.Length.ToString());

                    return File(visualizationBytes, "image/png");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критична помилка в контролері аналізу.");
                return StatusCode(500, new { Error = "Внутрішня помилка сервера.", Details = ex.Message });
            }
            finally
            {
                if (cleanStrips != null)
                {
                    cleanStrips.ForEach(strip => strip.Dispose());
                }
            }
        }
    }
}