using System.Text;
using ECG_API.Core.Services;
using ECG_API.Core.MLModels;
using ECG_API.Core.Models;
using ECG_API.Infrastructure.Auth;
using ECG_API.Infrastructure.DependencyInjection;
using ECG_API.Infrastructure.MachineLearning;
using ECG_API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

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
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>(); 
builder.Services.AddDbContext<EcgDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("EcgDb")));
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key не задано. Виконайте: dotnet user-secrets set \"Jwt:Key\" \"$(openssl rand -base64 48)\" --project ECG_API");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
            Name = "Authorization", Description = "JWT from /api/auth/login"
        });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference {Type = ReferenceType.SecurityScheme, Id = "Bearer"},
            },
            Array.Empty<string>()
        }
    });
});

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

app.UseAuthentication();
app.UseAuthorization();
/*app.UseHttpsRedirection();*/

app.MapControllers();

app.Run();
