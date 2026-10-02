using ECG_API.Core.Services;
using ECG_API.Core.MLModels;
using ECG_API.Infrastructure.DependencyInjection;
using ECG_API.Infrastructure.MachineLearning;
using Microsoft.Extensions.ML; 

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEcgProcessing();
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
