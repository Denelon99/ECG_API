using System.ComponentModel.DataAnnotations;
using ECG_API.Core.Models;
using ECG_API.Core.Services;
using ECG_API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECG_API.Controllers;
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly EcgDbContext _dbContext;                                                                                                                                                                                    
    private readonly IPasswordHasher<User> _hasher;                                                                                                                                                                              
    private readonly ITokenService _tokenService;

    public AuthController(EcgDbContext dbContext, IPasswordHasher<User> hasher, ITokenService tokenService)
    {
        _dbContext = dbContext;
        _hasher = hasher;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _dbContext.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict("HTTP 409");
        }
        var user = new User {Email = email, CreatedAtUtc = DateTime.UtcNow};
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        try
        {
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict("HTTP 409");
        }
        return StatusCode(StatusCodes.Status201Created);
    }
    [HttpPost("Login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user == null || _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) ==
            PasswordVerificationResult.Failed)
        {
            return Unauthorized();
        }
        return Ok(new {token = _tokenService.CreateToken(user)});
    }
}
public class RegisterRequest
{
    [Required, EmailAddress] public required string Email { get; set; }
    [Required, MinLength(8)] public required string Password { get; set; }
}
public class LoginRequest
{
    [Required] public required string Email { get; set; }
    [Required] public required string Password { get; set; }
}
