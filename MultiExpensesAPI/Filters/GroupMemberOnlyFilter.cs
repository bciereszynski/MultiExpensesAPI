using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using MultiExpensesAPI.Services;

namespace MultiExpensesAPI.Filters;

public class GroupMemberOnlyFilter : IAsyncActionFilter
{
    private readonly IGroupsService _service;

    public GroupMemberOnlyFilter(IGroupsService service)
    {
        _service = service;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ActionArguments.TryGetValue("id", out object? idObject) &&
            !context.ActionArguments.TryGetValue("groupId", out idObject))
        {
            await next();
            return;
        }

        if (idObject is not int groupId)
        {
            context.Result = new BadRequestObjectResult("Invalid group ID format.");
            return;
        }

        var userIdClaim = context.HttpContext.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        bool isFound = await _service.GetByIdAsync(groupId) != null;

        if (!isFound)
        {
            context.Result = new NotFoundResult();
            return;
        }

        bool isMember = await _service.IsUserMemberOfGroupAsync(userId, groupId);

        if (!isMember)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}