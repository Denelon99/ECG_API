using System.Text;
using ECG_API.Core.Models;
using ECG_API.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ECG_API.Tests;

public class JwtTokenServiceTests
{
    private const string TestKey = "itsomenewtestkeyforjswtunixtests";

    private static JwtTokenService CreateService(Dictionary<string, string?> settings)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new JwtTokenService(config);
    }
    [Fact]
    public void CreateToken_NoKey_Throws()
    {
        var testService = CreateService(new Dictionary<string, string?>());
        User testUser = new User { Email = "test@test.com" };
        Assert.Throws<InvalidOperationException>(() => testService.CreateToken(testUser));
    }
    [Fact]
    public async Task CreateToken_ValidConfig_ReturnsValidToken()
    {
        var testService = CreateService(new Dictionary<string, string?>
            { ["Jwt:Key"] = TestKey, ["Jwt:Issuer"] = "test-issuer", ["Jwt:Audience"] = "test-audience", ["Jwt:ExpiresMinutes"] = "60" });
        User testUser = new User { Email = "test@test.com" };
        var validation = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "test-issuer",
            ValidateAudience = true,
            ValidAudience = "test-audience",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        };
        var handler = new JsonWebTokenHandler();
        
        var result = await handler.ValidateTokenAsync(testService.CreateToken(testUser),validation);
        Assert.True(result.IsValid);
    }
    
}
