using ECG_API.Infrastructure.ImageProcessing; 
using ECG_API.Core.Services;
using ECG_API.Infrastructure.Storage;
using ECG_API.Core.MLModels;
using ECG_API.Infrastructure.MachineLearning;
using Microsoft.Extensions.ML; 

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<IEcgWorkingAreaService, EcgWorkingAreaService>();
builder.Services.AddScoped<ICalibrationPulseRemoverService, CalibrationPulseRemoverService>();
builder.Services.AddScoped<IFeatureExtractionService, FeatureExtractionService>();
builder.Services.AddScoped<IEcgStripSplitterService, EcgStripSplitterService>();
builder.Services.AddScoped<IEcgSignalIsolationService, EcgSignalIsolationService>();
builder.Services.AddScoped<IEcgSignalConnectorService, EcgSignalConnectorService>();
builder.Services.AddScoped<IEcgArtifactRemoverService, EcgArtifactRemoverService>();
builder.Services.AddScoped<IEcgBaselineService, EcgBaselineService>();
builder.Services.AddScoped<IEcgVisualizerService, EcgVisualizerService>();
builder.Services.AddScoped<IEcgPreprocessorService, EcgPreprocessorService>();
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
builder.Services.AddScoped<IFeatureCalculatorService, FeatureCalculatorService>();
builder.Services.AddScoped<IGainLineDetectorService, GainLineDetectorService>();
builder.Services.AddScoped<IHistogramVisualizerService, HistogramVisualizerService>();
builder.Services.AddPredictionEnginePool<ModelInput, ModelOutput>()
    .FromFile(
        modelName: "EcgClassificationModel", 
        filePath: "Core/MLModels/EcgClassifier.zip",
        watchForChanges: true
    );

builder.Services.AddScoped<IPredictionService, PredictionService>();

builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    
    app.UseSwaggerUI();
}

/*app.UseHttpsRedirection();*/

app.MapControllers();

app.Run();