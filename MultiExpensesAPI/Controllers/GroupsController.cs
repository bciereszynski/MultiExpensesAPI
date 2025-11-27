using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using MultiExpensesAPI.Dtos;
using MultiExpensesAPI.Filters;
using MultiExpensesAPI.Services;
using System.Security.Claims;

namespace MultiExpensesAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableCors("AllowAll")]
[Authorize]
public class GroupsController(IGroupsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        int userId = GetUserIdFromClaims();
        var allGroups = await service.GetAllAsync(userId);
        return Ok(allGroups);
    }

    [ServiceFilter(typeof(GroupMemberOnlyFilter))]
    [HttpGet("{id}", Name = "GetGroupById")]
    public async Task<IActionResult> GetById(int id)
    {
        var group = await service.GetByIdAsync(id);

        if (group == null)
        {
            return NotFound();
        }

        return Ok(group);
    }

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

    [ServiceFilter(typeof(GroupMemberOnlyFilter))]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PostGroupDto groupDto)
    {
        var updated = await service.UpdateAsync(id, groupDto);

        if (updated == null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    [ServiceFilter(typeof(GroupMemberOnlyFilter))]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await service.DeleteAsync(id);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    [ServiceFilter(typeof(GroupMemberOnlyFilter))]
    [HttpPost("{groupId}/members")]
    public async Task<IActionResult> AddMember(int groupId, [FromBody] AddMemberToGroupDto dto)
    {
        var result = await service.AddMemberAsync(groupId, dto.UserId);

        if (!result)
        {
            return BadRequest("Failed to add member. Group or user not found, or user already in group.");
        }

        return Ok();
    }

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

    [ServiceFilter(typeof(GroupMemberOnlyFilter))]
    [HttpGet("{groupId}/members")]
    public async Task<IActionResult> GetMembers(int groupId)
    {
        var members = await service.GetMembersAsync(groupId);

        return Ok(members);
    }

    [ServiceFilter(typeof(GroupMemberOnlyFilter))]
    [HttpGet("{groupId}/transactions")]
    public async Task<IActionResult> GetGroupTransactions(int groupId)
    {
        var transactions = await service.GetGroupTransactionsAsync(groupId);

        return Ok(transactions);
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