using ECG_API.Core.Services;
using ECG_API.Core.MLModels;
using ECG_API.Infrastructure.DependencyInjection;
using ECG_API.Infrastructure.MachineLearning;
using ECG_API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddDbContext<EcgDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("EcgDb")));
builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    EcgDbContext dbContext = scope.ServiceProvider.GetRequiredService<EcgDbContext>();
    dbContext.Database.Migrate();
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    
    app.UseSwaggerUI();
}

/*app.UseHttpsRedirection();*/

app.MapControllers();

app.Run();
