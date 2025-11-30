using Microsoft.EntityFrameworkCore;
using MultiExpensesAPI.Data;
using MultiExpensesAPI.Dtos;
using MultiExpensesAPI.Models;

namespace MultiExpensesAPI.Services;

public interface ITransactionsService
{
    Task<List<Transaction>> GetAllByGroupAsync(int groupId);
    Task<Transaction?> GetByIdAsync(int id, int groupId);
    Task<Transaction> AddAsync(PostTransactionDto transaction, int groupId);
    Task<Transaction?> UpdateAsync(int id, PostTransactionDto transaction, int groupId);
    Task<bool> DeleteAsync(int id, int groupId);
    Task<double> GetExpensesByMemberAsync(int groupId, int memberId);
    Task<double> GetIncomeByMemberAsync(int groupId, int memberId);
}

public class TransactionsService(AppDbContext context) : ITransactionsService
{
    public async Task<List<Transaction>> GetAllByGroupAsync(int groupId)
    {
        return await context.Transactions
            .Where(x => x.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<Transaction?> GetByIdAsync(int id, int groupId)
    {
        return await context.Transactions
            .FirstOrDefaultAsync(n => n.Id == id && n.GroupId == groupId);
    }

    public async Task<Transaction> AddAsync(PostTransactionDto transactionDto, int groupId)
    {
        var groupExists = await context.Groups.AnyAsync(g => g.Id == groupId);
        if (!groupExists)
        {
            throw new ArgumentException("Group not found", nameof(groupId));
        }


        var newTransaction = new Transaction
        {
            Type = transactionDto.Type,
            Amount = transactionDto.Amount,
            Category = transactionDto.Category,
            Description = transactionDto.Description,
            CreatedAt = transactionDto.CreatedAt,
            LastUpdatedAt = DateTime.UtcNow,
            UserId = transactionDto.UserId,
            GroupId = groupId
        };

        await context.Transactions.AddAsync(newTransaction);
        await context.SaveChangesAsync();

        return newTransaction;
    }

    public async Task<Transaction?> UpdateAsync(int id, PostTransactionDto transaction, int groupId)
    {
        var transactionDb = await context.Transactions
            .FirstOrDefaultAsync(n => n.Id == id && n.GroupId == groupId);
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
        transactionDb.UserId = transaction.UserId;
        transactionDb.GroupId = groupId;

        context.Transactions.Update(transactionDb);
        await context.SaveChangesAsync();

        return transactionDb;
    }

    public async Task<bool> DeleteAsync(int id, int groupId)
    {
        var transactionDb = await context.Transactions
            .FirstOrDefaultAsync(n => n.Id == id && n.GroupId == groupId);
        if (transactionDb == null)
        {
            return false;
        }

        context.Transactions.Remove(transactionDb);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<double> GetExpensesByMemberAsync(int groupId, int memberId)
    {
        return await context.Transactions
            .Where(t => t.GroupId == groupId && t.UserId == memberId && t.Type.ToLower() == "expense")
            .SumAsync(t => t.Amount);
    }

    public async Task<double> GetIncomeByMemberAsync(int groupId, int memberId)
    {
        return await context.Transactions
            .Where(t => t.GroupId == groupId && t.UserId == memberId && t.Type.ToLower() == "earning")
            .SumAsync(t => t.Amount);
    }
}