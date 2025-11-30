using Microsoft.EntityFrameworkCore;
using MultiExpensesAPI.Data;
using MultiExpensesAPI.Dtos;

namespace MultiExpensesAPI.Services;

public interface IUsersService
{
    Task<UserDto?> FindByEmailAsync(string email);
}

public class UsersService(AppDbContext context) : IUsersService
{
    public async Task<UserDto?> FindByEmailAsync(string email)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        return user == null ? null : new UserDto(user.Id, user.Email);
    }
}