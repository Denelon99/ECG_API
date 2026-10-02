using ECG_API.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Security.Cryptography;
using System.Text;
using ECG_API.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration; 

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        // --- ДОДАЄМО ЯВНЕ ЗАВАНТАЖЕННЯ ---
        config.SetBasePath(Directory.GetCurrentDirectory());
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        Console.WriteLine($"[DEBUG] Завантаження конфігурації з: {Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")}");
        // --- КІНЕЦЬ ЗМІНИ ---
    }).ConfigureServices((context, services) =>
    {
        services.AddEcgProcessing();
        services.AddSingleton<IWebHostEnvironment, MockWebHostEnvironment>();
        services.AddTransient<DataGenerationTask>();
    })
    .Build();

var task = host.Services.GetRequiredService<DataGenerationTask>();
await task.RunAsync();

public class DataGenerationTask
{
    private readonly ILogger<DataGenerationTask> _logger;
    private readonly IConfiguration _config; 
    private readonly IEcgPreprocessorService _preprocessor;
    private readonly IFeatureExtractionService _featureExtractor;
    private readonly IImageStorageService _imageStorageService;
    private readonly IEcgVisualizerService _visualizerService;

    public DataGenerationTask(
        ILogger<DataGenerationTask> logger,
        IConfiguration config,
        IEcgPreprocessorService preprocessor,
        IFeatureExtractionService featureExtractor,
        IEcgVisualizerService visualizerService,
        IImageStorageService imageStorageService)
    {
        _logger = logger;
        _config = config; 
        _preprocessor = preprocessor;
        _featureExtractor = featureExtractor;
        _imageStorageService = imageStorageService;
        _visualizerService = visualizerService;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("--- Початок генерації датасетів ---");
        
        // var outputDirectory = _config.GetValue<string>("Paths:OutputCsvDirectory"); 
        var outputCsvPath = _config.GetValue<string>("Paths:OutputCsvFile"); 
        var saveVisualizations = _config.GetValue<bool>("Paths:SaveVisualizations");
        var inputDirectoriesSection = _config.GetSection("Paths:InputDirectories");
        
        if (string.IsNullOrEmpty(outputCsvPath) || !inputDirectoriesSection.Exists())
        {
            // Більш детальна помилка
            if (string.IsNullOrEmpty(outputCsvPath))
            {
                _logger.LogError("КРИТИЧНА ПОМИЛКА: Ключ 'Paths:OutputCsvDirectory' не знайдено або його значення порожнє в appsettings.json!");
            }
            if (!inputDirectoriesSection.Exists())
            {
                _logger.LogError("КРИТИЧНА ПОМИЛКА: Секція 'Paths:InputDirectories' не знайдена в appsettings.json!");
            }

            _logger.LogWarning("Перевірте, що файл 'appsettings.json' існує, має правильну структуру та властивість 'Copy to Output Directory' встановлена на 'Copy if newer'.");
            return;
        }
        
        Directory.CreateDirectory(Path.GetDirectoryName(outputCsvPath)!);

        // --- ПІДГОТОВКА ЄДИНОГО CSV ФАЙЛУ ---
        var csvBuilder = new StringBuilder();
        
        var headers = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 40; j++)
            {
                headers.Add($"Strip{i + 1}_Feature{j + 1}");
            }
        }
        headers.Add("Diagnosis"); // Додаємо стовпець для мітки класу
        csvBuilder.AppendLine(string.Join(",", headers));
        // string csvHeaderLine = string.Join(",", headers);
        
        // Датасет містить побайтово однакові копії (напр. MI(n) == MI(n+30)).
        // Пропускаємо їх, щоб одна картинка не потрапила і в train, і в test (data leakage).
        // Ключ — SHA-256 файлу, значення — перший файл з таким хешем.
        var seenHashes = new Dictionary<string, string>();
        int skippedDuplicates = 0;

        // --- ОБРОБКА ДАНИХ ТА ЗАПИС В ОДИН ФАЙЛ ---
        foreach (var directoryEntry in inputDirectoriesSection.GetChildren())
        {
            string diagnosis = directoryEntry.Key;
            string dirPath = directoryEntry.Value!; 
            
            _logger.LogInformation(">>> Обробка класу: {Diagnosis}...", diagnosis);
            
            // var csvBuilder = new StringBuilder();
            // csvBuilder.AppendLine(csvHeaderLine); 
            
            if (!Directory.Exists(dirPath))
            {
                _logger.LogWarning("   Папка для класу '{Diagnosis}' не знайдена, пропускаємо.", diagnosis);
                continue;
            }

            var imageFiles = Directory.GetFiles(dirPath, "*.jpg")
                .Concat(Directory.GetFiles(dirPath, "*.png"));
            
            foreach (var filePath in imageFiles)
            {
                try
                {
                    var fileBytes = await File.ReadAllBytesAsync(filePath);
                    var hash = Convert.ToHexString(SHA256.HashData(fileBytes));

                    if (!seenHashes.TryAdd(hash, filePath))
                    {
                        skippedDuplicates++;
                        _logger.LogInformation("   Дублікат пропущено: {File} (= {Original})",
                            Path.GetFileName(filePath), Path.GetFileName(seenHashes[hash]));
                        continue;
                    }

                    _logger.LogInformation("   Обробка файлу: {File}", Path.GetFileName(filePath));

                    using var image = Image.Load<Rgba32>(fileBytes);
                    var preprocessingResult = await _preprocessor.PreprocessAsync(image);

                    using (preprocessingResult.FullCleanImage)
                    {
                        var cleanStrips = preprocessingResult.Strips;

                        if (cleanStrips.Any())
                        {
                            var features = _featureExtractor.ExtractFeatures(cleanStrips);
                            
                            var featureString = string.Join(",", features.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                            csvBuilder.AppendLine($"{featureString},{diagnosis}");
                            
                            _logger.LogInformation("   Ознаки для класу '{Diagnosis}' успішно додано до CSV.", diagnosis);

                            if (saveVisualizations)
                            {
                                string originalFileName = Path.GetFileNameWithoutExtension(filePath);
                                // Зберігаємо її
                                await _imageStorageService.SaveImageAsync(preprocessingResult.FullCleanImage,
                                    $"{originalFileName}_processed",
                                    Path.Combine("visualizations", diagnosis));
                                
                                _logger.LogInformation("   Візуалізацію успішно збережено.");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("   Не вдалося обробити файл (не знайдено смуг): {File}", Path.GetFileName(filePath));
                        }

                        cleanStrips.ForEach(strip => strip.Dispose());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "   Критична помилка при обробці файлу {File}", Path.GetFileName(filePath));
                }
            }
            
            // string outputCsvPath = Path.Combine(outputDirectory, $"{diagnosis}.csv");
            // await File.WriteAllTextAsync(outputCsvPath, csvBuilder.ToString());
            // _logger.LogInformation(">>> Обробку класу {Diagnosis} завершено. Файл збережено: {Path}", diagnosis, outputCsvPath);
        }
        await File.WriteAllTextAsync(outputCsvPath, csvBuilder.ToString());
        _logger.LogInformation("--- Унікальних зображень: {Unique}, пропущено дублікатів: {Skipped}",
            seenHashes.Count, skippedDuplicates);
        _logger.LogInformation("--- Генерацію об'єднаного датасету завершено. Файл збережено: {Path}", outputCsvPath);
    }
}

public class MockWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = string.Empty;
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ApplicationName { get; set; } = "EcgDataGenerator";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public string EnvironmentName { get; set; } = "Development";
}
