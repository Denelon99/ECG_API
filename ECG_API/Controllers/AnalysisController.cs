using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using ECG_API.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ECG_API.Controllers
{
    public class AnalyzeImageRequest
    {
        [Required] public required IFormFile ImageFile { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController(
        ILogger<AnalysisController> logger,
        IEcgPreprocessorService preprocessorService,
        IFeatureExtractionService featureExtractionService,
        IEcgVisualizerService visualizerService) : ControllerBase
    {
        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeImage([FromForm] AnalyzeImageRequest request)
        {
            try
            {
                await using var imageStream = request.ImageFile.OpenReadStream();
                using var originalImage = await Image.LoadAsync<Rgba32>(imageStream);

                var cleanStrips = preprocessorService.Preprocess(originalImage);

                if (cleanStrips == null || cleanStrips.Count == 0)
                {
                    return StatusCode(500, "Помилка на етапі попередньої обробки.");
                }

                var features = featureExtractionService.ExtractFeatures(cleanStrips);

                using var firstStrip = cleanStrips.FirstOrDefault()?.Clone();
                byte[]? visualizationBytes = null;
                if (firstStrip != null)
                {
                    var baselineService = HttpContext.RequestServices.GetRequiredService<IEcgBaselineService>();
                    int baseline = baselineService.FindBaseline(firstStrip);

                    using var visualImage = visualizerService.CreateVisualisation(firstStrip, baseline);
                    using var ms = new MemoryStream();
                    await visualImage.SaveAsPngAsync(ms);
                    visualizationBytes = ms.ToArray();
                }

                cleanStrips.ForEach(strip => strip.Dispose());

                logger.LogInformation("Фінальний вектор ознак: [{Features}]", string.Join(", ", features));

                if (visualizationBytes != null)
                {
                    return File(visualizationBytes, "image/png");
                }

                return Ok(new { Message = "Обробка завершена, але візуалізація не створена." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Критична помилка в контролері аналізу.");
                return StatusCode(500, "Внутрішня помилка сервера.");
            }
        }
    }
}