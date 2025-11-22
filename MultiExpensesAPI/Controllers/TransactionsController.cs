using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using MultiExpensesAPI.Dtos;
using MultiExpensesAPI.Services;

namespace MultiExpensesAPI.Controllers;
[Route("api/[controller]")]
[ApiController]
[EnableCors("AllowAll")]
public class TransactionsController(ITransactionsService service) : ControllerBase
{
    // GET api/Transactions
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var allTransactions = await service.GetAllAsync();
        return Ok(allTransactions);
    }

    // GET api/Transactions/{id}
    [HttpGet("{id}", Name = "GetTransactionById")]
    public async Task<IActionResult> GetById(int id)
    {
        var foundTransaction = await service.GetByIdAsync(id);
        if (foundTransaction == null)
        {
            return NotFound();
        }
        return Ok(foundTransaction);
    }

    // POST api/Transactions
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PostTransactionDto transactionDto)
    {
        var newTransaction = await service.AddAsync(transactionDto);

        return CreatedAtRoute("GetTransactionById", new { id = newTransaction.Id }, newTransaction);
    }

    // PUT api/Transactions/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PostTransactionDto transactionDto)
    {
        var updatedTransaction = await service.UpdateAsync(id, transactionDto);
        if (updatedTransaction == null)
        {
            return NotFound();
        }
        return Ok(updatedTransaction);
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
}
