using System.Security.Claims;
using System.Text;
using ECG_API.Core.Models;
using ECG_API.Core.Services;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ECG_API.Infrastructure.Auth;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key не задано. Виконайте: dotnet user-secrets set \"Jwt:Key\" \"$(openssl rand -base64 48)\" --project ECG_API")));
        var descriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new  Claim(JwtRegisteredClaimNames.Email, user.Email ),
            }),
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            Expires = DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:ExpiresMinutes")),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
