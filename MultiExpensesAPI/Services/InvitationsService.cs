using Microsoft.EntityFrameworkCore;
using MultiExpensesAPI.Data;
using MultiExpensesAPI.Dtos;
using MultiExpensesAPI.Models;
using System.Security.Cryptography;

namespace MultiExpensesAPI.Services;


public interface IInvitationsService
{
    Task<CreateInvitationResponseDto?> CreateInvitationAsync(int groupId, int inviterId);
    Task<bool> AcceptInvitationAsync(string token, int userId);
    Task<bool> RevokeInvitationAsync(int groupId, string token);
}

public class InvitationsService(AppDbContext context, IMembersService membersService) : IInvitationsService
{
    public async Task<CreateInvitationResponseDto?> CreateInvitationAsync(int groupId, int inviterId)
    {
        var group = await context.Groups.FindAsync(groupId);
        if (group == null) return null;

        var isInviterMember = await membersService.IsUserMemberOfGroupAsync(inviterId, groupId);
        if (!isInviterMember) return null;

        var token = GenerateSecureToken();
        var invitation = new GroupInvitation
        {
            GroupId = groupId,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            LastUpdatedAt = DateTime.UtcNow
        };

        context.GroupInvitations.Add(invitation);
        await context.SaveChangesAsync();

        return new CreateInvitationResponseDto(token, invitation.ExpiresAt);
    }

    public async Task<bool> AcceptInvitationAsync(string token, int userId)
    {
        var invitation = await context.GroupInvitations
            .Include(i => i.Group)
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invitation == null || invitation.ExpiresAt < DateTime.UtcNow)
            return false;

        var isAlreadyMember = await membersService.IsUserMemberOfGroupAsync(userId, invitation.GroupId);
        if (isAlreadyMember) return false;

        var user = await context.Users.FindAsync(userId);
        if (user == null) return false;

        invitation.Group.Members.Add(user);

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RevokeInvitationAsync(int groupId, string token)
    {
        var invitation = await context.GroupInvitations
            .FirstOrDefaultAsync(i => i.Token == token && i.GroupId == groupId);

        if (invitation == null) return false;

        context.GroupInvitations.Remove(invitation);
        await context.SaveChangesAsync();
        return true;
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}