using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace ECG_API.Infrastructure.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                  throw new InvalidOperationException("Error with user Id");
        return int.Parse(sub);
    }
}
