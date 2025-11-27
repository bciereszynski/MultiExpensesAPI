using Microsoft.EntityFrameworkCore;
using MultiExpensesAPI.Data;
using MultiExpensesAPI.Dtos;
using MultiExpensesAPI.Models;

namespace MultiExpensesAPI.Services;

public interface ITransactionsService
{
    Task<List<Transaction>> GetAllAsync(int userId);
    Task<Transaction?> GetByIdAsync(int id);
    Task<Transaction> AddAsync(PostTransactionDto transaction, int userId);
    Task<Transaction?> UpdateAsync(int id, PostTransactionDto transaction);
    Task<bool> DeleteAsync(int id);
}

public class TransactionsService(AppDbContext context) : ITransactionsService
{

    public async Task<List<Transaction>> GetAllAsync(int userId)
    {
        return await context.Transactions.Where(x => x.UserId == userId).ToListAsync();
    }

    public async Task<Transaction?> GetByIdAsync(int id)
    {
        return await context.Transactions.FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<Transaction> AddAsync(PostTransactionDto transactionDto, int userId)
    {
        var groupExists = await context.Groups.AnyAsync(g => g.Id == transactionDto.GroupId);
        if (!groupExists)
        {
            throw new ArgumentException("Group not found", nameof(transactionDto.GroupId));
        }

        var newTransaction = new Transaction
        {
            Type = transactionDto.Type,
            Amount = transactionDto.Amount,
            Category = transactionDto.Category,
            Description = transactionDto.Description,
            CreatedAt = transactionDto.CreatedAt,
            LastUpdatedAt = DateTime.UtcNow,
            UserId = userId,
            GroupId = transactionDto.GroupId
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

        var groupExists = await context.Groups.AnyAsync(g => g.Id == transaction.GroupId);
        if (!groupExists)
        {
            throw new ArgumentException("Group not found", nameof(transaction.GroupId));
        }

        transactionDb.Type = transaction.Type;
        transactionDb.Amount = transaction.Amount;
        transactionDb.Category = transaction.Category;
        transactionDb.Description = transaction.Description;
        transactionDb.CreatedAt = transaction.CreatedAt;
        transactionDb.LastUpdatedAt = DateTime.UtcNow;
        transactionDb.GroupId = transaction.GroupId;

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