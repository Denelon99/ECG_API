using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ECG_API.Tests;

public class AuthEndpointsTests(EcgApiFactory factory) : IClassFixture<EcgApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    
    private const string TestPassword = "testPassword";

    private record LoginResponse(string Token);

    [Fact]
    public async Task Register_NewEmail_Returns201()
    {
        string testEmail = $"{Guid.NewGuid():N}@test.com";
        
        var response = await _client.PostAsJsonAsync("/api/auth/register", new {Email = testEmail, Password = TestPassword});
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
    
    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        string testEmail = $"{Guid.NewGuid():N}@test.com";
        
        await _client.PostAsJsonAsync("/api/auth/register", new {Email = testEmail, Password = TestPassword});
        var response = await _client.PostAsJsonAsync("/api/auth/register", new {Email = testEmail, Password = TestPassword});
        
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        string shortPassword = "1234567";
        string testEmail = $"{Guid.NewGuid():N}@test.com";
        
        var response = await _client.PostAsJsonAsync("/api/auth/register", new {Email = testEmail, Password = shortPassword});
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        string testEmail = $"{Guid.NewGuid():N}@test.com";
        
        await _client.PostAsJsonAsync("/api/auth/register", new {Email = testEmail, Password = TestPassword});
        var response = await _client.PostAsJsonAsync("/api/auth/login", new {Email = testEmail, Password = TestPassword});
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        
        Assert.False(string.IsNullOrEmpty(body?.Token));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        string testEmail = $"{Guid.NewGuid():N}@test.com";
        string wrongPassword = "wrongPassword";
        
        await _client.PostAsJsonAsync("/api/auth/register", new {Email = testEmail, Password = TestPassword});
        var response = await _client.PostAsJsonAsync("/api/auth/login", new {Email = testEmail, Password = wrongPassword});
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task History_WithToken_Returns200()
    {
        string testEmail = $"{Guid.NewGuid():N}@test.com";
        
        await _client.PostAsJsonAsync("/api/auth/register", new {Email = testEmail, Password = TestPassword});
        var tokenResponse = await _client.PostAsJsonAsync("/api/auth/login", new {Email = testEmail, Password = TestPassword});
        
        var body = await tokenResponse.Content.ReadFromJsonAsync<LoginResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Token);
        var response = await _client.GetAsync("api/analysis/history");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task History_NoToken_Returns401()
    {
        var response = await _client.GetAsync("api/analysis/history");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
