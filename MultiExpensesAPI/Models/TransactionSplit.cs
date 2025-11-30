using MultiExpensesAPI.Models.Base;

namespace MultiExpensesAPI.Models;

public class TransactionSplit : Entity
{
    public required int TransactionId { get; set; }
    public virtual Transaction? Transaction { get; set; }

    public required int UserId { get; set; }
    public virtual User? User { get; set; }

    public double Amount { get; set; }
}