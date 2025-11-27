using Microsoft.EntityFrameworkCore;
using MultiExpensesAPI.Data;
using MultiExpensesAPI.Dtos;
using MultiExpensesAPI.Models;

namespace MultiExpensesAPI.Services;

public interface IGroupsService
{
    Task<List<Group>> GetAllAsync(int userId);
    Task<Group?> GetByIdAsync(int id);
    Task<Group> AddAsync(PostGroupDto groupDto, int creatorUserId);
    Task<Group?> UpdateAsync(int id, PostGroupDto groupDto);
    Task<bool> DeleteAsync(int id);
    Task<bool> AddMemberAsync(int groupId, int userId);
    Task<bool> RemoveMemberAsync(int groupId, int userId);
    Task<List<UserDto>> GetMembersAsync(int groupId);
    Task<List<Transaction>> GetGroupTransactionsAsync(int groupId);
    Task<bool> IsUserMemberOfGroupAsync(int userId, int groupId);
}

public class GroupsService(AppDbContext context) : IGroupsService
{
    public async Task<List<Group>> GetAllAsync(int userId)
    {
        return await context.Groups
            .Include(g => g.Members)
            .Where(g => g.Members.Any(m => m.Id == userId))
            .ToListAsync();
    }

    public async Task<Group?> GetByIdAsync(int id)
    {
        var group = await context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
        {
            return null;
        }

        return group;
    }

    public async Task<Group> AddAsync(PostGroupDto groupDto, int creatorUserId)
    {
        var creator = await context.Users.FindAsync(creatorUserId);
        if (creator == null)
        {
            throw new ArgumentException("User not found", nameof(creatorUserId));
        }

        var newGroup = new Group
        {
            Name = groupDto.Name,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            Members = new List<User> { creator }
        };

        await context.Groups.AddAsync(newGroup);
        await context.SaveChangesAsync();

        return newGroup;
    }

    public async Task<Group?> UpdateAsync(int id, PostGroupDto groupDto)
    {
        var group = await GetByIdAsync(id);
        if (group == null)
        {
            return null;
        }

        group.Name = groupDto.Name;
        group.LastUpdatedAt = DateTime.UtcNow;

        context.Groups.Update(group);
        await context.SaveChangesAsync();

        return group;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var group = await GetByIdAsync(id);

        if (group == null)
        {
            return false;
        }

        context.Groups.Remove(group);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddMemberAsync(int groupId, int userId)
    {
        var group = await context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        var user = await context.Users.FindAsync(userId);

        if (group == null || user == null)
        {
            return false;
        }

        if (group.Members.Any(m => m.Id == userId))
        {
            return false;
        }

        group.Members.Add(user);
        group.LastUpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveMemberAsync(int groupId, int userId)
    {
        var group = await GetByIdAsync(groupId);

        if (group == null)
        {
            return false;
        }

        var member = group.Members.FirstOrDefault(m => m.Id == userId);
        if (member == null)
        {
            return false;
        }

        group.Members.Remove(member);
        group.LastUpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserDto>> GetMembersAsync(int groupId)
    {
        var group = await GetByIdAsync(groupId);

        return group?.Members.Select(m => new UserDto(m.Id, m.Email)).ToList()
           ?? new List<UserDto>();
    }

    public async Task<List<Transaction>> GetGroupTransactionsAsync(int groupId)
    {
        return await context.Transactions
            .Where(t => t.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<bool> IsUserMemberOfGroupAsync(int userId, int groupId)
    {
        return await context.Groups
            .Where(g => g.Id == groupId)
            .AnyAsync(g => g.Members.Any(m => m.Id == userId));
    }
}