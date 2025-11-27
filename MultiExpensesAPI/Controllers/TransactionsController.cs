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
public class TransactionsController(ITransactionsService service) : ControllerBase
{
    // GET api/Transactions
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        int userId = GetUserIdFromClaims();
        var allTransactions = await service.GetAllAsync(userId);
        return Ok(allTransactions);
    }

    // GET api/Transactions/{id}
    [HttpGet("{id}", Name = "GetTransactionById")]
    public async Task<IActionResult> GetById(int id)
    {
        // TODO - allow only for people in the group
        try
        {
            var foundTransaction = await service.GetByIdAsync(id);
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

    // POST api/Transactions
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PostTransactionDto transactionDto)
    {

        int userId = GetUserIdFromClaims();
        try
        {
            var newTransaction = await service.AddAsync(transactionDto, userId);
            return CreatedAtRoute("GetTransactionById", new { id = newTransaction.Id }, newTransaction);
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }

    }

    // PUT api/Transactions/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PostTransactionDto transactionDto)
    {
        try
        {
            var updatedTransaction = await service.UpdateAsync(id, transactionDto);
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

    // DELETE api/Transactions/{id}
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
