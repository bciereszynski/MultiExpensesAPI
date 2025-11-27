using Microsoft.AspNetCore.Mvc.Testing;
using MultiExpensesAPI;
using MultiExpensesAPI.Dtos;
using MultiExpensesAPI.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Tests;

public class GroupsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GroupsTests(WebApplicationFactory<Program> factory)
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

    [Fact]
    public async Task CreateGroup_WithAuth_ReturnsCreated()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthorizationHeader(token);

        var dto = new
        {
            name = "Test Group"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Group>();
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Equal("Test Group", created.Name);
    }

    [Fact]
    public async Task CreateGroup_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        var dto = new
        {
            name = "Unauthorized Group"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateGroup_AutomaticallyAddsCreatorAsMember()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthorizationHeader(token);

        var dto = new
        {
            name = "Creator Auto-Add Group"
        };

        // Act
        var createResponse = await _client.PostAsJsonAsync("/api/groups", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<Group>();

        var membersResponse = await _client.GetAsync($"/api/groups/{created!.Id}/members");
        var members = await membersResponse.Content.ReadFromJsonAsync<List<UserDto>>();

        // Assert
        Assert.NotNull(members);
        Assert.Single(members);
    }

    [Fact]
    public async Task GetAllGroups_WithAuth_ReturnsOnlyUserGroups()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthorizationHeader(token);

        var group1 = new { name = "Group 1" };
        var group2 = new { name = "Group 2" };
        
        await _client.PostAsJsonAsync("/api/groups", group1);
        await _client.PostAsJsonAsync("/api/groups", group2);

        // Act
        var response = await _client.GetAsync("/api/groups");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var groups = await response.Content.ReadFromJsonAsync<List<Group>>();
        Assert.NotNull(groups);
        Assert.Equal(2, groups!.Count);
    }

    [Fact]
    public async Task GetAllGroups_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/groups");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetGroupById_WithAuth_ExistingGroup_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthorizationHeader(token);

        var dto = new { name = "Specific Group" };
        var createResponse = await _client.PostAsJsonAsync("/api/groups", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<Group>();

        // Act
        var response = await _client.GetAsync($"/api/groups/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var group = await response.Content.ReadFromJsonAsync<Group>();
        Assert.NotNull(group);
        Assert.Equal(created.Id, group!.Id);
        Assert.Equal("Specific Group", group.Name);
    }

    [Fact]
    public async Task GetGroupById_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange - No token set
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/groups/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetGroupById_NonExistingGroup_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync("/api/groups/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGroup_WithAuth_ExistingGroup_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthorizationHeader(token);

        var createDto = new { name = "Original Name" };
        var createResponse = await _client.PostAsJsonAsync("/api/groups", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<Group>();

        // Act
        var updateDto = new { name = "Updated Name" };
        var response = await _client.PutAsJsonAsync($"/api/groups/{created!.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<Group>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal("Updated Name", updated.Name);
    }

    [Fact]
    public async Task UpdateGroup_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange - No token set
        _client.DefaultRequestHeaders.Authorization = null;

        var updateDto = new { name = "Should be unauthorized" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/groups/1", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGroup_NonExistingGroup_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthorizationHeader(token);

        var updateDto = new { name = "Non-existent" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/groups/99999", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGroup_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.DeleteAsync("/api/groups/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGroup_NonExistingGroup_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthorizationHeader(token);

        // Act
        var response = await _client.DeleteAsync("/api/groups/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddMember_ValidUser_ReturnsOk()
    {
        // Arrange
        var ownerToken = await GetAuthTokenAsync();
        SetAuthorizationHeader(ownerToken);

        var groupDto = new { name = "Shared Group" };
        var groupResponse = await _client.PostAsJsonAsync("/api/groups", groupDto);
        var group = await groupResponse.Content.ReadFromJsonAsync<Group>();

        var memberToken = await GetAuthTokenAsync();
        var memberUserDto = new
        {
            email = $"member_{Guid.NewGuid()}@example.com",
            password = "MemberPassword123!"
        };
        var memberResponse = await _client.PostAsJsonAsync("/api/auth/register", memberUserDto);
        var memberContent = await memberResponse.Content.ReadAsStringAsync();
        var memberTokenResponse = JsonSerializer.Deserialize<JsonElement>(memberContent);
        
        SetAuthorizationHeader(memberToken);
        var tempGroupResponse = await _client.PostAsJsonAsync("/api/groups", new { name = "Temp" });
        var tempGroup = await tempGroupResponse.Content.ReadFromJsonAsync<Group>();
        var membersListResponse = await _client.GetAsync($"/api/groups/{tempGroup!.Id}/members");
        var membersList = await membersListResponse.Content.ReadFromJsonAsync<List<UserDto>>();
        var memberId = membersList![0].Id;

        SetAuthorizationHeader(ownerToken);

        // Act
        var addMemberDto = new { userId = memberId };
        var response = await _client.PostAsJsonAsync($"/api/groups/{group!.Id}/members", addMemberDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedMembersResponse = await _client.GetAsync($"/api/groups/{group.Id}/members");
        var updatedMembers = await updatedMembersResponse.Content.ReadFromJsonAsync<List<UserDto>>();
        Assert.Equal(2, updatedMembers!.Count);
    }

    [Fact]
    public async Task AddMember_InvalidUser_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthorizationHeader(token);

        var groupDto = new { name = "Test Group" };
        var groupResponse = await _client.PostAsJsonAsync("/api/groups", groupDto);
        var group = await groupResponse.Content.ReadFromJsonAsync<Group>();

        // Act
        var addMemberDto = new { userId = 99999 }; // Non-existent user
        var response = await _client.PostAsJsonAsync($"/api/groups/{group!.Id}/members", addMemberDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RemoveMember_ExistingMember_ReturnsNoContent()
    {
        // Arrange
        var ownerToken = await GetAuthTokenAsync();
        SetAuthorizationHeader(ownerToken);

        var groupDto = new { name = "Group for Removal" };
        var groupResponse = await _client.PostAsJsonAsync("/api/groups", groupDto);
        var group = await groupResponse.Content.ReadFromJsonAsync<Group>();

        var memberToken = await GetAuthTokenAsync();
        SetAuthorizationHeader(memberToken);
        var tempGroupResponse = await _client.PostAsJsonAsync("/api/groups", new { name = "Temp" });
        var tempGroup = await tempGroupResponse.Content.ReadFromJsonAsync<Group>();
        var membersListResponse = await _client.GetAsync($"/api/groups/{tempGroup!.Id}/members");
        var membersList = await membersListResponse.Content.ReadFromJsonAsync<List<UserDto>>();
        var memberId = membersList![0].Id;

        SetAuthorizationHeader(ownerToken);
        var addMemberDto = new { userId = memberId };
        await _client.PostAsJsonAsync($"/api/groups/{group!.Id}/members", addMemberDto);

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/members/{memberId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var updatedMembersResponse = await _client.GetAsync($"/api/groups/{group.Id}/members");
        var updatedMembers = await updatedMembersResponse.Content.ReadFromJsonAsync<List<UserDto>>();
        Assert.Single(updatedMembers!);
    }

    [Fact]
    public async Task GetGroupMembers_ReturnsAllMembers()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthorizationHeader(token);

        var groupDto = new { name = "Members Test Group" };
        var groupResponse = await _client.PostAsJsonAsync("/api/groups", groupDto);
        var group = await groupResponse.Content.ReadFromJsonAsync<Group>();

        // Act
        var response = await _client.GetAsync($"/api/groups/{group!.Id}/members");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var members = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        Assert.NotNull(members);
        Assert.Single(members); // Creator is automatically added
    }

    [Fact]
    public async Task UserCanOnlySeeGroupsTheyBelongTo()
    {
        // Arrange
        var user1Token = await GetAuthTokenAsync();
        SetAuthorizationHeader(user1Token);
        var group1Response = await _client.PostAsJsonAsync("/api/groups", new { name = "User 1 Group" });
        var group1 = await group1Response.Content.ReadFromJsonAsync<Group>();

        var user2Token = await GetAuthTokenAsync();
        SetAuthorizationHeader(user2Token);
        await _client.PostAsJsonAsync("/api/groups", new { name = "User 2 Group" });

        // Act
        SetAuthorizationHeader(user1Token);
        var response = await _client.GetAsync("/api/groups");
        var groups = await response.Content.ReadFromJsonAsync<List<Group>>();

        // Assert
        Assert.NotNull(groups);
        Assert.Single(groups);
        Assert.Equal(group1!.Id, groups![0].Id);
    }

    [Fact]
    public async Task AddMember_DuplicateMember_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthorizationHeader(token);

        var groupDto = new { name = "Duplicate Test Group" };
        var groupResponse = await _client.PostAsJsonAsync("/api/groups", groupDto);
        var group = await groupResponse.Content.ReadFromJsonAsync<Group>();

        var membersResponse = await _client.GetAsync($"/api/groups/{group!.Id}/members");
        var members = await membersResponse.Content.ReadFromJsonAsync<List<UserDto>>();
        var memberId = members![0].Id;

        // Act
        var addMemberDto = new { userId = memberId };
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/members", addMemberDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}