using ECG_API.Core.Services;
using ECG_API.Infrastructure.Extensions;
using ECG_API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECG_API.Controllers;
[ApiController]
[Authorize]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly EcgDbContext _dbContext;
    private readonly IImageStorageService _imageStorage;

    public AccountController(EcgDbContext context, IImageStorageService imageStorage)
    {
        _dbContext = context;
        _imageStorage = imageStorage;
    }
    
    [HttpDelete]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.GetUserId();
        var user = await _dbContext.Users.FindAsync(userId);
        if (user is null)
        {
            return NotFound();
        }
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
        _imageStorage.DeleteUserImage(user.Id);
        return NoContent();
    }
}
