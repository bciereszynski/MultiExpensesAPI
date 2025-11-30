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
[ServiceFilter(typeof(GroupMemberOnlyFilter))]
public class TransactionsController(ITransactionsService service) : ControllerBase
{
    // GET api/groups/{groupId}/transactions
    [HttpGet]
    public async Task<IActionResult> GetAll(int groupId)
    {
        var allTransactions = await service.GetAllByGroupAsync(groupId);
        return Ok(allTransactions);
    }

    // GET api/groups/{groupId}/transactions/{id}
    [HttpGet("{id}", Name = "GetTransactionById")]
    public async Task<IActionResult> GetById(int groupId, int id)
    {
        try
        {
            var foundTransaction = await service.GetByIdAsync(id, groupId);
            if (foundTransaction == null)
            {
                return NotFound();
            }
            return Ok(foundTransaction);
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    // POST api/groups/{groupId}/transactions
    [HttpPost]
    public async Task<IActionResult> Create(int groupId, [FromBody] PostTransactionDto transactionDto)
    {
        try
        {
            var newTransaction = await service.AddAsync(transactionDto, groupId);
            return CreatedAtRoute("GetTransactionById", new { groupId, id = newTransaction.Id }, newTransaction);
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    // PUT api/groups/{groupId}/transactions/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int groupId, int id, [FromBody] PostTransactionDto transactionDto)
    {        
        try
        {
            var updatedTransaction = await service.UpdateAsync(id, transactionDto, groupId);
            if (updatedTransaction == null)
            {
                return NotFound();
            }
            return Ok(updatedTransaction);
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    // DELETE api/groups/{groupId}/transactions/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int groupId, int id)
    {
        var result = await service.DeleteAsync(id, groupId);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    // GET api/groups/{groupId}/transactions/expenses/{memberId}
    [HttpGet("expenses/{memberId}")]
    public async Task<IActionResult> GetExpensesSum(int groupId, int memberId)
    {
        var sum = await service.GetExpensesByMemberAsync(groupId, memberId);
        return Ok(new { memberId, groupId, expensesSum = sum });
    }

    // GET api/groups/{groupId}/transactions/earnings/{memberId}
    [HttpGet("income/{memberId}")]
    public async Task<IActionResult> GetEarningsSum(int groupId, int memberId)
    {
        var sum = await service.GetIncomeByMemberAsync(groupId, memberId);
        return Ok(new { memberId, groupId, earningsSum = sum });
    }
}