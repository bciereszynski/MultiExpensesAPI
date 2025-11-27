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

        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{groupId}/transactions", dto);

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
    public async Task CreateTransaction_WithInvalidGroupId_ReturnsNotFound()
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
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/99999/transactions", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        var dto = new
        {
            type = "expense",
            amount = 10.0,
            category = "Test",
            description = "Unit test",
            createdAt = DateTime.UtcNow,
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups/1/transactions", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithAuth_ReturnsOnlyGroupTransactions()
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
        };
        await _client.PostAsJsonAsync($"/api/groups/{groupId}/transactions", dto);

        // Act
        var response = await _client.GetAsync($"/api/groups/{groupId}/transactions");

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
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/groups/1/transactions");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_UserNotInGroup_ReturnsForbidden()
    {
        // Arrange
        var user1Token = await GetAuthTokenAsync();
        var groupId = await CreateGroupAsync(user1Token);

        var user2Token = await GetAuthTokenAsync();
        SetAuthorizationHeader(user2Token);

        // Act
        var response = await _client.GetAsync($"/api/groups/{groupId}/transactions");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

        };
        var createResponse = await _client.PostAsJsonAsync($"/api/groups/{groupId}/transactions", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<Transaction>();

        // Act
        var response = await _client.GetAsync($"/api/groups/{groupId}/transactions/{created!.Id}");

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
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/groups/1/transactions/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_UserNotInGroup_ReturnsForbidden()
    {
        // Arrange
        var user1Token = await GetAuthTokenAsync();
        var groupId = await CreateGroupAsync(user1Token);
        SetAuthorizationHeader(user1Token);

        var dto = new
        {
            type = "expense",
            amount = 25.0,
            category = "Food",
            description = "Groceries",
            createdAt = DateTime.UtcNow,

        };
        var createResponse = await _client.PostAsJsonAsync($"/api/groups/{groupId}/transactions", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<Transaction>();

        var user2Token = await GetAuthTokenAsync();
        SetAuthorizationHeader(user2Token);

        // Act
        var response = await _client.GetAsync($"/api/groups/{groupId}/transactions/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_TransactionNotInGroup_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var group1Id = await CreateGroupAsync(token, "Group 1");
        var group2Id = await CreateGroupAsync(token, "Group 2");
        SetAuthorizationHeader(token);

        var dto = new
        {
            type = "expense",
            amount = 25.0,
            category = "Food",
            description = "Groceries",
            createdAt = DateTime.UtcNow,
        };
        var createResponse = await _client.PostAsJsonAsync($"/api/groups/{group1Id}/transactions", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<Transaction>();

        // Act
        var response = await _client.GetAsync($"/api/groups/{group2Id}/transactions/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithAuth_NonExistingTransaction_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var groupId = await CreateGroupAsync(token);
        SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync($"/api/groups/{groupId}/transactions/99999");

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

        };
        var createResponse = await _client.PostAsJsonAsync($"/api/groups/{groupId}/transactions", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<Transaction>();

        // Act
        var updateDto = new
        {
            type = "expense",
            amount = 35.0,
            category = "Transport",
            description = "Train ticket",
            createdAt = created!.CreatedAt,

        };
        var response = await _client.PutAsJsonAsync($"/api/groups/{groupId}/transactions/{created.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<Transaction>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal(35.0, updated.Amount);
        Assert.Equal("Train ticket", updated.Description);
    }

    [Fact]
    public async Task Update_WithMismatchedGroupId_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var group1Id = await CreateGroupAsync(token, "Group 1");
        var group2Id = await CreateGroupAsync(token, "Group 2");
        SetAuthorizationHeader(token);

        var createDto = new
        {
            type = "expense",
            amount = 30.0,
            category = "Transport",
            description = "Bus ticket",
            createdAt = DateTime.UtcNow,
        };
        var createResponse = await _client.PostAsJsonAsync($"/api/groups/{group1Id}/transactions", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<Transaction>();

        // Act
        var updateDto = new
        {
            type = "expense",
            amount = 35.0,
            category = "Transport",
            description = "Train ticket",
            createdAt = created!.CreatedAt,
        };
        var response = await _client.PutAsJsonAsync($"/api/groups/{group2Id}/transactions/{created.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/groups/1/transactions/1", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_UserNotInGroup_ReturnsForbidden()
    {
        // Arrange
        var user1Token = await GetAuthTokenAsync();
        var groupId = await CreateGroupAsync(user1Token);
        SetAuthorizationHeader(user1Token);

        var createDto = new
        {
            type = "expense",
            amount = 30.0,
            category = "Transport",
            description = "Bus ticket",
            createdAt = DateTime.UtcNow,

        };
        var createResponse = await _client.PostAsJsonAsync($"/api/groups/{groupId}/transactions", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<Transaction>();

        var user2Token = await GetAuthTokenAsync();
        SetAuthorizationHeader(user2Token);

        var updateDto = new
        {
            type = "expense",
            amount = 35.0,
            category = "Transport",
            description = "Train ticket",
            createdAt = created!.CreatedAt,

        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{groupId}/transactions/{created.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{groupId}/transactions/99999", updateDto);

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
        };
        var createResponse = await _client.PostAsJsonAsync($"/api/groups/{groupId}/transactions", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<Transaction>();

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{groupId}/transactions/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/groups/{groupId}/transactions/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.DeleteAsync("/api/groups/1/transactions/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_UserNotInGroup_ReturnsForbidden()
    {
        // Arrange
        var user1Token = await GetAuthTokenAsync();
        var groupId = await CreateGroupAsync(user1Token);
        SetAuthorizationHeader(user1Token);

        var dto = new
        {
            type = "expense",
            amount = 15.0,
            category = "Entertainment",
            description = "Movie ticket",
            createdAt = DateTime.UtcNow,

        };
        var createResponse = await _client.PostAsJsonAsync($"/api/groups/{groupId}/transactions", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<Transaction>();

        var user2Token = await GetAuthTokenAsync();
        SetAuthorizationHeader(user2Token);

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{groupId}/transactions/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithAuth_NonExistingTransaction_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var groupId = await CreateGroupAsync(token);
        SetAuthorizationHeader(token);

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{groupId}/transactions/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GroupsIsolateTransactions()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var group1Id = await CreateGroupAsync(token, "Group 1");
        var group2Id = await CreateGroupAsync(token, "Group 2");
        SetAuthorizationHeader(token);

        var group1Transaction = new
        {
            type = "expense",
            amount = 100.0,
            category = "Groceries",
            description = "Group 1 groceries",
            createdAt = DateTime.UtcNow,
        };
        await _client.PostAsJsonAsync($"/api/groups/{group1Id}/transactions", group1Transaction);

        var group2Transaction = new
        {
            type = "income",
            amount = 200.0,
            category = "Salary",
            description = "Group 2 salary",
            createdAt = DateTime.UtcNow,
        };
        await _client.PostAsJsonAsync($"/api/groups/{group2Id}/transactions", group2Transaction);

        // Act
        var group1Response = await _client.GetAsync($"/api/groups/{group1Id}/transactions");
        var group1Transactions = await group1Response.Content.ReadFromJsonAsync<List<Transaction>>();

        var group2Response = await _client.GetAsync($"/api/groups/{group2Id}/transactions");
        var group2Transactions = await group2Response.Content.ReadFromJsonAsync<List<Transaction>>();

        // Assert
        Assert.NotNull(group1Transactions);
        Assert.Single(group1Transactions);
        Assert.Equal("Groceries", group1Transactions![0].Category);
        Assert.Equal(100.0, group1Transactions[0].Amount);

        Assert.NotNull(group2Transactions);
        Assert.Single(group2Transactions);
        Assert.Equal("Salary", group2Transactions![0].Category);
        Assert.Equal(200.0, group2Transactions[0].Amount);
    }
}