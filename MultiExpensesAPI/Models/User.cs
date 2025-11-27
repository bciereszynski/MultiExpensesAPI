using MultiExpensesAPI.Models.Base;

namespace MultiExpensesAPI.Models
{
    public class User : Entity
    {
        public required string Email { get; set; }
        public required string Password { get; set; }

        public virtual ICollection<Transaction>? Transactions { get; set; }
    }
}
