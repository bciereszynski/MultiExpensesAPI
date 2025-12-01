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
    Task<double> GetPaidByMemberAsync(int groupId, int memberId);
}

public class TransactionsService(AppDbContext context) : ITransactionsService
{
    public async Task<List<Transaction>> GetAllByGroupAsync(int groupId)
    {
        return await context.Transactions
            .Include(t => t.Splits)
            .Where(x => x.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<Transaction?> GetByIdAsync(int id, int groupId)
    {
        return await context.Transactions
            .Include(t => t.Splits)
            .FirstOrDefaultAsync(n => n.Id == id && n.GroupId == groupId);
    }

    public async Task<Transaction> AddAsync(PostTransactionDto transactionDto, int groupId)
    {
        var groupExists = await context.Groups.AnyAsync(g => g.Id == groupId);
        if (!groupExists)
        {
            throw new ArgumentException("Group not found", nameof(groupId));
        }

        if (transactionDto.Splits != null && transactionDto.Splits.Count > 0)
        {
            await ValidateSplitsAsync(transactionDto.Splits, groupId, transactionDto.Amount);
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

        if (transactionDto.Splits != null && transactionDto.Splits.Count > 0)
        {
            foreach (var splitDto in transactionDto.Splits)
            {
                var split = new TransactionSplit
                {
                    TransactionId = newTransaction.Id,
                    UserId = splitDto.UserId,
                    Amount = splitDto.Amount,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                };
                await context.TransactionSplits.AddAsync(split);
            }
            await context.SaveChangesAsync();
        }

        return await GetByIdAsync(newTransaction.Id, groupId) ?? newTransaction;
    }

    public async Task<Transaction?> UpdateAsync(int id, PostTransactionDto transaction, int groupId)
    {
        var transactionDb = await context.Transactions
            .Include(t => t.Splits)
            .FirstOrDefaultAsync(n => n.Id == id && n.GroupId == groupId);
        if (transactionDb == null)
        {
            return null;
        }

        if (transaction.Splits != null && transaction.Splits.Count > 0)
        {
            await ValidateSplitsAsync(transaction.Splits, groupId, transaction.Amount);
        }

        transactionDb.Type = transaction.Type;
        transactionDb.Amount = transaction.Amount;
        transactionDb.Category = transaction.Category;
        transactionDb.Description = transaction.Description;
        transactionDb.CreatedAt = transaction.CreatedAt;
        transactionDb.LastUpdatedAt = DateTime.UtcNow;
        transactionDb.UserId = transaction.UserId;
        transactionDb.GroupId = groupId;

        context.TransactionSplits.RemoveRange(transactionDb.Splits);

        if (transaction.Splits != null && transaction.Splits.Count > 0)
        {
            foreach (var splitDto in transaction.Splits)
            {
                var split = new TransactionSplit
                {
                    TransactionId = transactionDb.Id,
                    UserId = splitDto.UserId,
                    Amount = splitDto.Amount,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                };
                transactionDb.Splits.Add(split);
            }
        }

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
        var expenseSplits = await context.TransactionSplits
            .Where(ts => ts.UserId == memberId 
                && ts.Transaction!.GroupId == groupId 
                && ts.Transaction.Type.ToLower() == "expense")
            .SumAsync(ts => ts.Amount);

        var incomeSplits = await context.TransactionSplits
            .Where(ts => ts.UserId == memberId 
                && ts.Transaction!.GroupId == groupId 
                && ts.Transaction.Type.ToLower() == "income")
            .SumAsync(ts => ts.Amount);

        return expenseSplits - incomeSplits;
    }

    public async Task<double> GetPaidByMemberAsync(int groupId, int memberId)
    {
        var expensesPaid = await context.Transactions
            .Where(t => t.GroupId == groupId 
                && t.UserId == memberId 
                && t.Type.ToLower() == "expense")
            .SumAsync(t => t.Amount);

        var incomeTransactions = await context.Transactions
            .Where(t => t.GroupId == groupId 
                && t.UserId == memberId 
                && t.Type.ToLower() == "income")
            .SumAsync(t => t.Amount);

        return expensesPaid - incomeTransactions;
    }

    private async Task ValidateSplitsAsync(List<TransactionSplitDto> splits, int groupId, double totalAmount)
    {
        var userIds = splits.Select(s => s.UserId).Distinct().ToList();
        var group = await context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
        {
            throw new ArgumentException("Group not found", nameof(groupId));
        }

        var groupMemberIds = group.Members.Select(m => m.Id).ToHashSet();
        var invalidUserIds = userIds.Where(uid => !groupMemberIds.Contains(uid)).ToList();

        if (invalidUserIds.Count > 0)
        {
            throw new ArgumentException($"Users with IDs {string.Join(", ", invalidUserIds)} are not members of this group");
        }

        var splitSum = splits.Sum(s => s.Amount);
        var tolerance = 0.01;

        if (Math.Abs(splitSum - totalAmount) > tolerance)
        {
            throw new ArgumentException($"Split amounts ({splitSum:F2}) must equal the transaction amount ({totalAmount:F2})");
        }

        if (splits.Any(s => s.Amount < 0))
        {
            throw new ArgumentException("Split amounts cannot be negative");
        }
    }
}