using Microsoft.AspNetCore.Mvc.Testing;
using MultiExpensesAPI;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Tests;

public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidUser_ReturnsOkWithToken()
    {
        // Arrange
        var userDto = new
        {
            email = $"test_{Guid.NewGuid()}@example.com",
            password = "SecurePassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", userDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(tokenResponse.TryGetProperty("token", out var token));
        Assert.False(string.IsNullOrEmpty(token.GetString()));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var email = $"duplicate_{Guid.NewGuid()}@example.com";
        var userDto = new
        {
            email = email,
            password = "Password123!"
        };

        // Register first user
        await _client.PostAsJsonAsync("/api/auth/register", userDto);

        // Act - Try to register with same email
        var response = await _client.PostAsJsonAsync("/api/auth/register", userDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorMessage = await response.Content.ReadAsStringAsync();
        Assert.Contains("Email already in use", errorMessage);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var email = $"login_{Guid.NewGuid()}@example.com";
        var password = "MyPassword123!";
        var registerDto = new
        {
            email = email,
            password = password
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Act
        var loginDto = new
        {
            email = email,
            password = password
        };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(tokenResponse.TryGetProperty("token", out var token));
        Assert.False(string.IsNullOrEmpty(token.GetString()));
    }

    [Fact]
    public async Task Login_InvalidEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new
        {
            email = "nonexistent@example.com",
            password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var errorMessage = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid credentials", errorMessage);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var email = $"wrongpass_{Guid.NewGuid()}@example.com";
        var correctPassword = "CorrectPassword123!";
        var registerDto = new
        {
            email = email,
            password = correctPassword
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Act
        var loginDto = new
        {
            email = email,
            password = "WrongPassword123!"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var errorMessage = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid credentials", errorMessage);
    }

    [Fact]
    public async Task Register_ThenLogin_BothReturnValidTokens()
    {
        // Arrange
        var email = $"fullflow_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var userDto = new
        {
            email = email,
            password = password
        };

        // Act
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", userDto);
        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        var registerToken = JsonSerializer.Deserialize<JsonElement>(registerContent);

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", userDto);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginToken = JsonSerializer.Deserialize<JsonElement>(loginContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        Assert.True(registerToken.TryGetProperty("token", out var regToken));
        Assert.True(loginToken.TryGetProperty("token", out var logToken));
        
        Assert.False(string.IsNullOrEmpty(regToken.GetString()));
        Assert.False(string.IsNullOrEmpty(logToken.GetString()));

        Assert.Equal(logToken.GetString(), regToken.GetString());
    }

    [Fact]
    public async Task Register_MultipleUsers_AllGetUniqueTokens()
    {
        // Arrange & Act
        var tokens = new List<string>();
        
        for (int i = 0; i < 3; i++)
        {
            var userDto = new
            {
                email = $"user{i}_{Guid.NewGuid()}@example.com",
                password = $"Password{i}123!"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", userDto);
            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);
            
            tokenResponse.TryGetProperty("token", out var token);
            tokens.Add(token.GetString()!);
        }

        // Assert
        Assert.Equal(3, tokens.Count);
        Assert.Equal(3, tokens.Distinct().Count()); // All tokens should be unique
    }
}