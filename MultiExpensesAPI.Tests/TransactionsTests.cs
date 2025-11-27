using Microsoft.AspNetCore.Mvc.Testing;
using MultiExpensesAPI.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tests;

public class TransactionsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TransactionsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var userDto = new
        {
            email = $"testuser_{Guid.NewGuid()}@example.com",
            password = "TestPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", userDto);
        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);
        
        tokenResponse.TryGetProperty("token", out var token);
        return token.GetString()!;
    }

    private void SetAuthorizationHeader(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<int> CreateGroupAsync(string token, string groupName = "Test Group")
    {
        SetAuthorizationHeader(token);
        
        var groupDto = new
        {
            name = groupName
        };

        var response = await _client.PostAsJsonAsync("/api/groups", groupDto);
        var group = await response.Content.ReadFromJsonAsync<Group>();
        return group!.Id;
    }

    [Fact]
    public async Task CreateTransaction_WithValidGroupId_ReturnsCreated()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var groupId = await CreateGroupAsync(token);
        SetAuthorizationHeader(token);

        var dto = new
        {
            type = "expense",
            amount = 10.0,
            category = "Test",
            description = "Unit test",
            createdAt = DateTime.UtcNow,
            groupId = groupId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Transaction>();
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Equal(groupId, created.GroupId);
        Assert.Equal(10.0, created.Amount);
        Assert.Equal("Test", created.Category);
    }

    [Fact]
    public async Task CreateTransaction_WithInvalidGroupId_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthorizationHeader(token);

        var dto = new
        {
            type = "expense",
            amount = 10.0,
            category = "Test",
            description = "Unit test",
            createdAt = DateTime.UtcNow,
            groupId = 99999
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange - No token set
        _client.DefaultRequestHeaders.Authorization = null;

        var dto = new
        {
            type = "expense",
            amount = 10.0,
            category = "Test",
            description = "Unit test",
            createdAt = DateTime.UtcNow,
            groupId = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithAuth_ReturnsOnlyUserTransactions()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var groupId = await CreateGroupAsync(token);
        SetAuthorizationHeader(token);

        var dto = new
        {
            type = "income",
            amount = 50.0,
            category = "Salary",
            description = "Monthly salary",
            createdAt = DateTime.UtcNow,
            groupId
        };
        await _client.PostAsJsonAsync("/api/transactions", dto);

        // Act
        var response = await _client.GetAsync("/api/transactions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var transactions = await response.Content.ReadFromJsonAsync<List<Transaction>>();
        Assert.NotNull(transactions);
        Assert.NotEmpty(transactions);
        Assert.All(transactions, t => Assert.Equal(groupId, t.GroupId));
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange - No token set
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/transactions");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithAuth_ExistingTransaction_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var groupId = await CreateGroupAsync(token);
        SetAuthorizationHeader(token);

        var dto = new
        {
            type = "expense",
            amount = 25.0,
            category = "Food",
            description = "Groceries",
            createdAt = DateTime.UtcNow,
            groupId = groupId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/transactions", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<Transaction>();

        // Act
        var response = await _client.GetAsync($"/api/transactions/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var transaction = await response.Content.ReadFromJsonAsync<Transaction>();
        Assert.NotNull(transaction);
        Assert.Equal(created.Id, transaction!.Id);
        Assert.Equal(25.0, transaction.Amount);
        Assert.Equal("Food", transaction.Category);
        Assert.Equal(groupId, transaction.GroupId);
    }

    [Fact]
    public async Task GetById_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange - No token set
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/transactions/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithAuth_NonExistingTransaction_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync("/api/transactions/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithAuth_ExistingTransaction_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var groupId = await CreateGroupAsync(token);
        SetAuthorizationHeader(token);

        var createDto = new
        {
            type = "expense",
            amount = 30.0,
            category = "Transport",
            description = "Bus ticket",
            createdAt = DateTime.UtcNow,
            groupId = groupId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/transactions", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<Transaction>();

        // Act
        var updateDto = new
        {
            type = "expense",
            amount = 35.0,
            category = "Transport",
            description = "Train ticket",
            createdAt = created!.CreatedAt,
            groupId = groupId
        };
        var response = await _client.PutAsJsonAsync($"/api/transactions/{created.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<Transaction>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal(35.0, updated.Amount);
        Assert.Equal("Train ticket", updated.Description);
    }

    [Fact]
    public async Task Update_WithInvalidGroupId_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var groupId = await CreateGroupAsync(token);
        SetAuthorizationHeader(token);

        var createDto = new
        {
            type = "expense",
            amount = 30.0,
            category = "Transport",
            description = "Bus ticket",
            createdAt = DateTime.UtcNow,
            groupId = groupId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/transactions", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<Transaction>();

        // Act
        var updateDto = new
        {
            type = "expense",
            amount = 35.0,
            category = "Transport",
            description = "Train ticket",
            createdAt = created!.CreatedAt,
            groupId = 99999 // Invalid group
        };
        var response = await _client.PutAsJsonAsync($"/api/transactions/{created.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange - No token set
        _client.DefaultRequestHeaders.Authorization = null;

        var updateDto = new
        {
            type = "expense",
            amount = 40.0,
            category = "Test",
            description = "Should be unauthorized",
            createdAt = DateTime.UtcNow,
            groupId = 1
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/transactions/1", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithAuth_NonExistingTransaction_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var groupId = await CreateGroupAsync(token);
        SetAuthorizationHeader(token);

        var updateDto = new
        {
            type = "expense",
            amount = 40.0,
            category = "Test",
            description = "Should not exist",
            createdAt = DateTime.UtcNow,
            groupId = groupId
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/transactions/99999", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithAuth_ExistingTransaction_ReturnsNoContent()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var groupId = await CreateGroupAsync(token);
        SetAuthorizationHeader(token);

        var dto = new
        {
            type = "expense",
            amount = 15.0,
            category = "Entertainment",
            description = "Movie ticket",
            createdAt = DateTime.UtcNow,
            groupId = groupId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/transactions", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<Transaction>();

        // Act
        var response = await _client.DeleteAsync($"/api/transactions/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/transactions/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange - No token set
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.DeleteAsync("/api/transactions/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithAuth_NonExistingTransaction_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthorizationHeader(token);

        // Act
        var response = await _client.DeleteAsync("/api/transactions/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UserCanOnlySeeOwnTransactions()
    {
        // Arrange
        var user1Token = await GetAuthTokenAsync();
        var user1GroupId = await CreateGroupAsync(user1Token, "User 1 Group");
        SetAuthorizationHeader(user1Token);

        var user1Transaction = new
        {
            type = "expense",
            amount = 100.0,
            category = "Groceries",
            description = "User 1 grocery shopping",
            createdAt = DateTime.UtcNow,
            groupId = user1GroupId
        };
        await _client.PostAsJsonAsync("/api/transactions", user1Transaction);

        var user2Token = await GetAuthTokenAsync();
        var user2GroupId = await CreateGroupAsync(user2Token, "User 2 Group");
        SetAuthorizationHeader(user2Token);

        var user2Transaction = new
        {
            type = "income",
            amount = 200.0,
            category = "Salary",
            description = "User 2 monthly salary",
            createdAt = DateTime.UtcNow,
            groupId = user2GroupId
        };
        var user2Response = await _client.PostAsJsonAsync("/api/transactions", user2Transaction);
        var user2CreatedTransaction = await user2Response.Content.ReadFromJsonAsync<Transaction>();

        // Act
        var getAllResponse = await _client.GetAsync("/api/transactions");
        var transactions = await getAllResponse.Content.ReadFromJsonAsync<List<Transaction>>();

        // Assert
        Assert.NotNull(transactions);
        Assert.Single(transactions);
        Assert.Equal(user2CreatedTransaction!.Id, transactions![0].Id);
        Assert.Equal("Salary", transactions[0].Category);
        Assert.Equal(200.0, transactions[0].Amount);
        Assert.Equal("income", transactions[0].Type);
        Assert.Equal(user2GroupId, transactions[0].GroupId);
    }

    [Fact]
    public async Task TransactionCannotBeMovedBetweenGroups()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var group1Id = await CreateGroupAsync(token, "Group 1");
        var group2Id = await CreateGroupAsync(token, "Group 2");
        SetAuthorizationHeader(token);

        var createDto = new
        {
            type = "expense",
            amount = 50.0,
            category = "Food",
            description = "Dinner",
            createdAt = DateTime.UtcNow,
            groupId = group1Id
        };
        var createResponse = await _client.PostAsJsonAsync("/api/transactions", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<Transaction>();

        // Act - Try to update to different group
        var updateDto = new
        {
            type = "expense",
            amount = 50.0,
            category = "Food",
            description = "Dinner",
            createdAt = created!.CreatedAt,
            groupId = group2Id
        };
        var updateResponse = await _client.PutAsJsonAsync($"/api/transactions/{created.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<Transaction>();
        Assert.Equal(group2Id, updated!.GroupId);
    }
}
