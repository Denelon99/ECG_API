using System.Security.Claims;
using ECG_API.Infrastructure.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ECG_API.Tests;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims) => new (new ClaimsIdentity(claims));

    [Fact]
    public void GetUserId_NoSubClaim_Throws()
    {
        var principal = CreatePrincipal(new Claim(JwtRegisteredClaimNames.Email, "a@test.com"));
        
        Assert.Throws<InvalidOperationException>(() => principal.GetUserId());
    }

    [Fact]
    public void GetUserId_ValidSub_ReturnsId()
    {
        var principal = CreatePrincipal(new Claim(JwtRegisteredClaimNames.Sub, "12345678"));
        
        int result = principal.GetUserId();
        
        Assert.Equal(12345678, result);
    }

    [Fact]
    public void GetUserId_NonNumericSub_Throws()
    {
        var principal = CreatePrincipal(new Claim(JwtRegisteredClaimNames.Sub, "a@test"));
        
        Assert.Throws<FormatException>(() => principal.GetUserId());
    }
}
