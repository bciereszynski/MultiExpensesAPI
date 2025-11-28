using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using MultiExpensesAPI.Dtos;
using MultiExpensesAPI.Filters;
using MultiExpensesAPI.Services;

namespace MultiExpensesAPI.Controllers;

[Route("api/Groups/{groupId}/[controller]")]
[ApiController]
[EnableCors("AllowAll")]
[Authorize]
[ServiceFilter(typeof(GroupMemberOnlyFilter))]
public class MembersController(IMembersService service) : ControllerBase
{
    // GET api/groups/{groupId}/members
    [HttpGet]
    public async Task<IActionResult> GetMembers(int groupId)
    {
        var members = await service.GetAsync(groupId);
        return Ok(members);
    }

    // POST api/groups/{groupId}/members
    [HttpPost]
    public async Task<IActionResult> AddMember(int groupId, [FromBody] AddMemberToGroupDto dto)
    {
        var result = await service.AddAsync(groupId, dto.UserId);

        if (!result)
        {
            return BadRequest("Failed to add member. Group or user not found, or user already in group.");
        }

        return Ok();
    }

    // DELETE api/groups/{groupId}/members/{userId}
    [HttpDelete("{userId}")]
    public async Task<IActionResult> RemoveMember(int groupId, int userId)
    {
        var result = await service.RemoveAsync(groupId, userId);
        
        if (!result)
        {
            return NotFound("Group or member not found.");
        }
        
        return NoContent();
    }
}