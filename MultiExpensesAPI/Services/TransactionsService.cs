using Microsoft.EntityFrameworkCore;
using MultiExpensesAPI.Dtos;
using MultiExpensesAPI.Models;
using MultiExpensesAPI.Data;

namespace MultiExpensesAPI.Services;

public interface ITransactionsService
{
    Task<List<Transaction>> GetAllAsync();
    Task<Transaction?> GetByIdAsync(int id);
    Task<Transaction> AddAsync(PostTransactionDto transaction);
    Task<Transaction?> UpdateAsync(int id, PostTransactionDto transaction);
    Task<bool> DeleteAsync(int id);
}

public class TransactionsService(AppDbContext context) : ITransactionsService
{

    public async Task<List<Transaction>> GetAllAsync()
    {
        return await context.Transactions.ToListAsync();
    }

    public async Task<Transaction?> GetByIdAsync(int id)
    {
        return await context.Transactions.FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<Transaction> AddAsync(PostTransactionDto transactionDto)
    {
        var newTransaction = new Transaction
        {
            Type = transactionDto.Type,
            Amount = transactionDto.Amount,
            Category = transactionDto.Category,
            Description = transactionDto.Description,
            CreatedAt = transactionDto.CreatedAt,
            LastUpdatedAt = DateTime.UtcNow
        };

        await context.Transactions.AddAsync(newTransaction);
        await context.SaveChangesAsync();

        return newTransaction;
    }

    public async Task<Transaction?> UpdateAsync(int id, PostTransactionDto transaction)
    {
        var transactionDb = await context.Transactions.FirstOrDefaultAsync(n => n.Id == id);
        if (transactionDb == null)
        {
            return null;
        }

        transactionDb.Type = transaction.Type;
        transactionDb.Amount = transaction.Amount;
        transactionDb.Category = transaction.Category;
        transactionDb.Description = transaction.Description;
        transactionDb.CreatedAt = transaction.CreatedAt;
        transactionDb.LastUpdatedAt = DateTime.UtcNow;

        context.Transactions.Update(transactionDb);
        await context.SaveChangesAsync();

        return transactionDb;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var transactionDb = await context.Transactions.FirstOrDefaultAsync(n => n.Id == id);
        if (transactionDb == null)
        {
            return false;
        }

        context.Transactions.Remove(transactionDb);
        await context.SaveChangesAsync();
        return true;
    }
}
