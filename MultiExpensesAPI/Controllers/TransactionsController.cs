using Microsoft.AspNetCore.Mvc;
using MultiExpensesAPI.Data;
using MultiExpensesAPI.Dtos;
using MultiExpensesAPI.Models;

namespace MultiExpensesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController(AppDbContext context) : ControllerBase
    {
        [HttpGet("All")]
        public IActionResult GetAll()
        {
            var allTransactions = context.Transactions.ToList();
            return Ok(allTransactions);
        }

        [HttpGet("Details/{id}")]
        public IActionResult GetById(int id)
        {
            var transactionDb = context.Transactions.FirstOrDefault(n => n.Id == id);
            if (transactionDb == null)
            {
                return NotFound();
            }

            return Ok(transactionDb);
        }

        [HttpPut("Update/{id}")]
        public IActionResult Update(int id, [FromBody] PostTransactionDto transactionDto)
        {
            var transactionDb = context.Transactions.FirstOrDefault(n => n.Id == id);
            if (transactionDb == null)
            {
                return NotFound();
            }

            transactionDb.Type = transactionDto.Type;
            transactionDb.Amount = transactionDto.Amount;
            transactionDb.Category = transactionDto.Category;
            transactionDb.LastUpdatedAt = DateTime.UtcNow;

            context.Transactions.Update(transactionDb);
            context.SaveChanges();
            return Ok(transactionDb);
        }

        [HttpDelete("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var transactionDb = context.Transactions.FirstOrDefault(n => n.Id == id);
            if (transactionDb == null)
            {
                return NotFound();
            }
            context.Transactions.Remove(transactionDb);
            context.SaveChanges();
            return Ok();
        }

        [HttpPost("Create")]
        public IActionResult Create([FromBody] PostTransactionDto transactionDto)
        {
            var newTransaction = new Transaction
            {
                Type = transactionDto.Type,
                Amount = transactionDto.Amount,
                Category = transactionDto.Category,
                Description = transactionDto.Description,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };
            context.Transactions.Add(newTransaction);
            context.SaveChanges();

            return Ok();
        }
    }
}
