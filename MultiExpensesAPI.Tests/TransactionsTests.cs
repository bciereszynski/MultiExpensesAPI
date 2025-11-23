using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using MultiExpensesAPI;
using MultiExpensesAPI.Models;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tests;
public class TransactionsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TransactionsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTransaction_ReturnsCreated()
    {
        var dto = new
        {
            type = "expense",
            amount = 10.0,
            category = "Test",
            description = "Unit test"
        };

        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Transaction>();
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithTransactions()
    {
        // Arrange
        var dto = new
        {
            type = "income",
            amount = 50.0,
            category = "Salary",
            description = "Monthly salary"
        };
        await _client.PostAsJsonAsync("/api/transactions", dto);

        // Act
        var response = await _client.GetAsync("/api/transactions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var transactions = await response.Content.ReadFromJsonAsync<List<Transaction>>();
        Assert.NotNull(transactions);
        Assert.NotEmpty(transactions);
    }

    [Fact]
    public async Task GetById_ExistingTransaction_ReturnsOk()
    {
        // Arrange
        var dto = new
        {
            type = "expense",
            amount = 25.0,
            category = "Food",
            description = "Groceries"
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
    }

    [Fact]
    public async Task GetById_NonExistingTransaction_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/transactions/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ExistingTransaction_ReturnsOk()
    {
        // Arrange
        var createDto = new
        {
            type = "expense",
            amount = 30.0,
            category = "Transport",
            description = "Bus ticket"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/transactions", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<Transaction>();

        // Act
        var updateDto = new
        {
            type = "expense",
            amount = 35.0,
            category = "Transport",
            description = "Train ticket"
        };
        var response = await _client.PutAsJsonAsync($"/api/transactions/{created!.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<Transaction>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal(35.0, updated.Amount);
        Assert.Equal("Train ticket", updated.Description);
    }

    [Fact]
    public async Task Update_NonExistingTransaction_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new
        {
            type = "expense",
            amount = 40.0,
            category = "Test",
            description = "Should not exist"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/transactions/99999", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingTransaction_ReturnsNoContent()
    {
        // Arrange
        var dto = new
        {
            type = "expense",
            amount = 15.0,
            category = "Entertainment",
            description = "Movie ticket"
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
    public async Task Delete_NonExistingTransaction_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/transactions/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
