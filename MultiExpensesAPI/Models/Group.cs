using MultiExpensesAPI.Models.Base;

namespace MultiExpensesAPI.Models
{
    public class Group : Entity
    {
        public required string Name { get; set; }
        
        public virtual ICollection<User> Members { get; set; } = new List<User>();
        
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}