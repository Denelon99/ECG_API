using ECG_API.Infrastructure.ImageProcessing; 
using ECG_API.Core.Services; 

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<IFeatureExtractionService, FeatureExtractionService>();

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