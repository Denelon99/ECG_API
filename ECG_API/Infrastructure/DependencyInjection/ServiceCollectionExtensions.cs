using ECG_API.Core.Services;
using ECG_API.Infrastructure.ImageProcessing;
using ECG_API.Infrastructure.Storage;

namespace ECG_API.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEcgProcessing(this IServiceCollection services)
    {
        services.AddScoped<IEcgWorkingAreaService, EcgWorkingAreaService>();
        services.AddScoped<ICalibrationPulseRemoverService, CalibrationPulseRemoverService>();
        services.AddScoped<IFeatureExtractionService, FeatureExtractionService>();
        services.AddScoped<IEcgStripSplitterService, EcgStripSplitterService>();
        services.AddScoped<IEcgSignalIsolationService, EcgSignalIsolationService>();
        services.AddScoped<IEcgSignalConnectorService, EcgSignalConnectorService>();
        services.AddScoped<IEcgArtifactRemoverService, EcgArtifactRemoverService>();
        services.AddScoped<IEcgBaselineService, EcgBaselineService>();
        services.AddScoped<IEcgVisualizerService, EcgVisualizerService>();
        services.AddScoped<IEcgPreprocessorService, EcgPreprocessorService>();
        services.AddScoped<IImageStorageService, ImageStorageService>();
        services.AddScoped<IFeatureCalculatorService, FeatureCalculatorService>();
        services.AddScoped<IGainLineDetectorService, GainLineDetectorService>();
        
        return services;
    }
}
