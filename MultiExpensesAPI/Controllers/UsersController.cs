using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using MultiExpensesAPI.Services;

namespace MultiExpensesAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableCors("AllowAll")]
[Authorize]
public class UsersController(IUsersService service) : ControllerBase
{
    // GET api/users/find?email=john@example.com
    [HttpGet("find")]
    public async Task<IActionResult> FindByEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email query parameter is required.");
        }

        var user = await service.FindByEmailAsync(email);
        
        if (user == null)
        {
            return NotFound("User not found.");
        }

        return Ok(user);
    }
}