using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using MultiExpensesAPI.Data.Services;
using MultiExpensesAPI.Dtos;

namespace MultiExpensesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAll")]
    public class TransactionsController(ITransactionsService service) : ControllerBase
    {
        [HttpGet("All")]
        public IActionResult GetAll()
        {
            var allTransactions = service.GetAll();
            return Ok(allTransactions);
        }

        [HttpGet("Details/{id}")]
        public IActionResult GetById(int id)
        {
            var foundTransaction = service.GetById(id);
            if (foundTransaction == null)
            {
                return NotFound();
            }
            return Ok(foundTransaction);
        }

        [HttpPut("Update/{id}")]
        public IActionResult Update(int id, [FromBody] PostTransactionDto transactionDto)
        {
            var updatedTransaction = service.Update(id, transactionDto);
            if (updatedTransaction == null)
            {
                return NotFound();
            }
            return Ok(updatedTransaction);
        }

        [HttpDelete("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var result = service.Delete(id);
            if (!result)
            {
                return NotFound();
            }
            return Ok();
        }

        [HttpPost("Create")]
        public IActionResult Create([FromBody] PostTransactionDto transactionDto)
        {
            var newTransaction = service.Add(transactionDto);

            return Ok(newTransaction);
        }
    }
}
