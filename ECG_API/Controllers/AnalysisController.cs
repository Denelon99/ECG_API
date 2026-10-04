using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ECG_API.Core.Services;
using ECG_API.Core.MLModels;
using ECG_API.Core.Models;
using ECG_API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using ECG_API.Infrastructure.Extensions;

namespace ECG_API.Controllers
{
    public class AnalyzeImageRequest
    {
        [Required]
        public required IFormFile ImageFile { get; set; }
    }
    
    [ApiController]
    [Authorize]
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
        private readonly EcgDbContext _dbContext;

        public AnalysisController(
            ILogger<AnalysisController> logger, 
            IEcgPreprocessorService preprocessorService,
            IFeatureExtractionService featureExtractionService,
            IPredictionService predictionService,
            IEcgVisualizerService visualizerService,
            IEcgBaselineService baselineService,
            IImageStorageService imageStorageService,
            EcgDbContext dbContext)
        {
            _logger = logger;
            _preprocessorService = preprocessorService;
            _featureExtractionService = featureExtractionService;
            _predictionService = predictionService;
            _visualizerService = visualizerService;
            _baselineService = baselineService;
            _imageStorageService = imageStorageService;
            _dbContext =  dbContext;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeImage([FromForm] AnalyzeImageRequest request)
        {
            var userId = User.GetUserId();
            var analysisId = Guid.NewGuid().ToString("N");
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
                        $"{analysisId}_clean", Path.Combine(userId.ToString(), "full_results"));

                    await _imageStorageService.SaveImageStripsAsync(cleanStrips, analysisId,
                        Path.Combine(userId.ToString(), "strips"));

                    var features = _featureExtractionService.ExtractFeatures(cleanStrips);

                    var modelInput = new ModelInput { Features = features };
                    var prediction = _predictionService.Predict(modelInput);
                    string predictedDiagnosis = prediction.PredictedDiagnosis;
                    
                    using var fullVisualImage = _visualizerService.CreateFullVisualisation(preprocessingResult);
                    
                     string resultPath = await _imageStorageService.SaveImageAsync(fullVisualImage,
                        $"{analysisId}_result", Path.Combine(userId.ToString(), "results"));
                     
                    var record = new AnalysisRecord
                    {
                        OriginalFileName = request.ImageFile.FileName,
                        PredictedDiagnosis = predictedDiagnosis,
                        Confidence = prediction.Score.Max(),
                        CreatedAtUtc = DateTime.UtcNow,
                        ResultImageFileName = Path.GetFileName(resultPath),
                        UserId = userId
                    };
                    _dbContext.AnalysisRecords.Add(record);
                    await _dbContext.SaveChangesAsync();
                    
                    using var ms = new MemoryStream();
                    await fullVisualImage.SaveAsPngAsync(ms);
                    byte[] visualizationBytes = ms.ToArray();
                    
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
