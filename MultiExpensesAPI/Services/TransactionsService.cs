using MultiExpensesAPI.Data;
using MultiExpensesAPI.Dtos;

using MultiExpensesAPI.Models;

namespace MultiExpensesAPI.Services;
public interface ITransactionsService
{
    List<Transaction> GetAll();
    Transaction? GetById(int id);
    Transaction Add(PostTransactionDto transaction);
    Transaction? Update(int id, PostTransactionDto transaction);
    bool Delete(int id);

}
public class TransactionsService(AppDbContext context) : ITransactionsService
{
    public List<Transaction> GetAll()
    {
        return context.Transactions.ToList();

    }
    public Transaction? GetById(int id)
    {
        return context.Transactions.FirstOrDefault(n => n.Id == id);
    }
    public Transaction Add(PostTransactionDto transactionDto)
    {
        var newTransaction = new Transaction()
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

        return newTransaction;

    }
    public Transaction? Update(int id, PostTransactionDto transaction)
    {
        var transactionDb = context.Transactions.FirstOrDefault(n => n.Id == id);
        if (transactionDb != null)
        {

            transactionDb.Type = transaction.Type;
            transactionDb.Amount = transaction.Amount;
            transactionDb.Category = transaction.Category;
            transactionDb.LastUpdatedAt = DateTime.UtcNow;

            context.Transactions.Update(transactionDb);
            context.SaveChanges();
        }

        return transactionDb;
    }

    public bool Delete(int id)
    {
        var transactionDb = context.Transactions.FirstOrDefault(n => n.Id == id);
        if (transactionDb != null)
        {
            context.Transactions.Remove(transactionDb);
            context.SaveChanges();
            return true;
        }
        return false;
    }
}
