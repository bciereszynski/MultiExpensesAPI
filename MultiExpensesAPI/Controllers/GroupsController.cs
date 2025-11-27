using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using MultiExpensesAPI.Dtos;
using MultiExpensesAPI.Services;
using System.Security.Claims;

namespace MultiExpensesAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableCors("AllowAll")]
[Authorize]
public class GroupsController(IGroupsService service) : ControllerBase
{
    // GET api/Groups
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        int userId = GetUserIdFromClaims();
        var allGroups = await service.GetAllAsync(userId);
        return Ok(allGroups);
    }

    // GET api/Groups/{id}
    [HttpGet("{id}", Name = "GetGroupById")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!await ValidateAccessToGroup(id))
        {
            return Forbid();
        }

        var group = await service.GetByIdAsync(id);
        
        if (group == null)
        {
            return NotFound();
        }
        
        return Ok(group);
    }

    // POST api/Groups
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PostGroupDto groupDto)
    {
        int userId = GetUserIdFromClaims();
        
        try
        {
            var newGroup = await service.AddAsync(groupDto, userId);
            return CreatedAtRoute("GetGroupById", new { id = newGroup.Id }, newGroup);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // PUT api/Groups/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PostGroupDto groupDto)
    {
        if (!await ValidateAccessToGroup(id))
        {
            return Forbid();
        }

        var updated = await service.UpdateAsync(id, groupDto);
        
        if (updated == null)
        {
            return NotFound();
        }
        
        return Ok(updated);
    }

    // DELETE api/Groups/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await ValidateAccessToGroup(id))
        {
            return Forbid();
        }

        var result = await service.DeleteAsync(id);
        
        if (!result)
        {
            return NotFound();
        }
        
        return NoContent();
    }

    // POST api/Groups/{groupId}/members
    [HttpPost("{groupId}/members")]
    public async Task<IActionResult> AddMember(int groupId, [FromBody] AddMemberToGroupDto dto)
    {
        if (!await ValidateAccessToGroup(groupId))
        {
            return Forbid();
        }

        var result = await service.AddMemberAsync(groupId, dto.UserId);
        
        if (!result)
        {
            return BadRequest("Failed to add member. Group or user not found, or user already in group.");
        }
        
        return Ok();
    }

    // DELETE api/Groups/{groupId}/members/{userId}
    [HttpDelete("{groupId}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(int groupId, int userId)
    {
        var result = await service.RemoveMemberAsync(groupId, userId);
        if (!result)
        {
            return NotFound("Group or member not found.");
        }
        return NoContent();
    }

    // GET api/Groups/{groupId}/members
    [HttpGet("{groupId}/members")]
    [Authorize]
    public async Task<IActionResult> GetMembers(int groupId)
    {

        if (!await ValidateAccessToGroup(groupId))
        {
            return Forbid();
        }
        
        var members = await service.GetMembersAsync(groupId);
        
        return Ok(members);
    }

    // GET api/Groups/{groupId}/transactions
    [HttpGet("{groupId}/transactions")]
    [Authorize]
    public async Task<IActionResult> GetGroupTransactions(int groupId)
    {
        var transactions = await service.GetGroupTransactionsAsync(groupId);
        
        return Ok(transactions);
    }

    private async Task<bool> ValidateAccessToGroup(int groupId)
    {
        int userId = GetUserIdFromClaims();

        return await service.IsUserMemberOfGroupAsync(userId, groupId);
    }

    private int GetUserIdFromClaims()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            throw new Exception("User ID claim not found.");
        }
        return int.Parse(userIdClaim.Value);
    }
}