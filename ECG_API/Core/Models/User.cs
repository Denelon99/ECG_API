namespace ECG_API.Core.Models;

public class User
{
    public int Id { get; private set; }
    public required string Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
