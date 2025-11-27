using MultiExpensesAPI.Models.Base;

namespace MultiExpensesAPI.Models
{
    public class User : Entity
    {
        public string Email { get; set; }
        public string Password { get; set; }

        public virtual ICollection<Transaction>? Transactions { get; set; }

    }
}
