using ECG_API.Core.Models;

namespace ECG_API.Core.Services;

public interface ITokenService
{
    string CreateToken(User user);
}
