using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using ECG_API.Core.Services;

namespace ECG_API.Controllers
{
    public class AnalyzeImageRequest
    {
        [Required] public required IFormFile ImageFile { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController(ILogger<AnalysisController> logger, 
        IFeatureExtractionService featureExtractionService) : ControllerBase
    {
        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeImage([FromForm] AnalyzeImageRequest request)
        {
            logger.LogInformation("Починається обробка файлу: {FileName}", request.ImageFile.FileName);
            
            await using var imageStream = request.ImageFile.OpenReadStream();
            
            // Викликаємо оновлений метод сервісу
            var (features, finalImageBytes) = await featureExtractionService.ProcessEcgImageAsync(imageStream);

            // Перевіряємо, чи повернулося зображення (масив байтів)
            if (finalImageBytes == null)
            {
                return StatusCode(500, "Не вдалося обробити зображення. Перевірте логи сервера.");
            }
            
            logger.LogInformation("Обробку завершено. Повертаємо зображення розміром {Size} байт.", finalImageBytes.Length);

            // --- ОСЬ КЛЮЧОВА ЗМІНА ---
            // Повертаємо файл зображення, а не JSON.
            // Перший параметр - масив байтів.
            // Другий - MIME-тип, щоб клієнт знав, що це картинка.
            return File(finalImageBytes, "image/png");
        }
    }
}