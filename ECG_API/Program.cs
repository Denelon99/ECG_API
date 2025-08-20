using ECG_API.Infrastructure.ImageProcessing; 
using ECG_API.Core.Services; 

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<IEcgWorkingAreaService, EcgWorkingAreaService>();
builder.Services.AddScoped<IFeatureExtractionService, FeatureExtractionService>();
builder.Services.AddScoped<IEcgStripSplitterService, EcgStripSplitterService>();
builder.Services.AddScoped<IEcgGridGeneratorService, EcgGridGeneratorService>();
builder.Services.AddScoped<ICalibrationPulseRemoverService, CalibrationPulseRemoverService>();
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
builder.Services.AddScoped<IEcgSignalIsolationService, EcgSignalIsolationService>();
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