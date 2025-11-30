using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using MultiExpensesAPI.Dtos;
using MultiExpensesAPI.Filters;
using MultiExpensesAPI.Services;
using System.Security.Claims;

namespace MultiExpensesAPI.Controllers;

[Route("api/Groups/{groupId}/[controller]")]
[ApiController]
[EnableCors("AllowAll")]
[Authorize]
public class InvitationsController(IInvitationsService service) : ControllerBase
{
    // GET api/groups/{groupId}/invitations
    [HttpGet]
    [ServiceFilter(typeof(GroupMemberOnlyFilter))]
    public async Task<IActionResult> GetActiveInvitations(int groupId)
    {
        var invitations = await service.GetActiveInvitationsAsync(groupId);
        return Ok(invitations);
    }

    // POST api/groups/{groupId}/invitations
    [HttpPost]
    [ServiceFilter(typeof(GroupMemberOnlyFilter))]
    public async Task<IActionResult> CreateInvitation(int groupId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var inviterId))
        {
            return Unauthorized();
        }

        var invitation = await service.CreateInvitationAsync(groupId, inviterId);

        if (invitation == null)
        {
            return BadRequest("Failed to create invitation. Group not found or you're not a member.");
        }

        return Ok(invitation);
    }

    // POST api/groups/invitations/accept
    [HttpPost("/api/groups/invitations/accept")]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await service.AcceptInvitationAsync(dto.Token, userId);

        if (!result)
        {
            return BadRequest("Invalid, expired, or already used invitation token.");
        }

        return Ok("Successfully joined the group.");
    }

    // DELETE api/groups/{groupId}/invitations/{token}
    [HttpDelete("{token}")]
    [ServiceFilter(typeof(GroupMemberOnlyFilter))]
    public async Task<IActionResult> RevokeInvitation(int groupId, string token)
    {
        var result = await service.RevokeInvitationAsync(groupId, token);

        if (!result)
        {
            return NotFound("Invitation not found.");
        }

        return NoContent();
    }
}