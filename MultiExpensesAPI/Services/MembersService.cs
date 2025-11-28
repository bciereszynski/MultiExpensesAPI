using Microsoft.EntityFrameworkCore;
using MultiExpensesAPI.Data;
using MultiExpensesAPI.Dtos;

namespace MultiExpensesAPI.Services;

public interface IMembersService
{
    Task<bool> AddAsync(int groupId, int userId);
    Task<bool> RemoveAsync(int groupId, int userId);
    Task<List<UserDto>> GetAsync(int groupId);
    Task<bool> IsUserMemberOfGroupAsync(int userId, int groupId);
}

public class MembersService(AppDbContext context) : IMembersService
{
    public async Task<bool> AddAsync(int groupId, int userId)
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

    public async Task<bool> RemoveAsync(int groupId, int userId)
    {
        var group = await context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

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

    public async Task<List<UserDto>> GetAsync(int groupId)
    {
        var group = await context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        return group?.Members.Select(m => new UserDto(m.Id, m.Email)).ToList()
            ?? new List<UserDto>();
    }

    public async Task<bool> IsUserMemberOfGroupAsync(int userId, int groupId)
    {
        return await context.Groups
            .Where(g => g.Id == groupId)
            .AnyAsync(g => g.Members.Any(m => m.Id == userId));
    }
}