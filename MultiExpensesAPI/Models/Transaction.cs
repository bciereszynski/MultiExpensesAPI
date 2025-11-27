using MultiExpensesAPI.Models.Base;

namespace MultiExpensesAPI.Models
{
    public class Transaction : Entity
    {
        public required string Type { get; set; }

        public double Amount { get; set; }

        public required string Category { get; set; }

        public string? Description { get; set; }

        public int? UserId { get; set;}
        public virtual User? User { get; set; }

        public int? GroupId { get; set; }
        public virtual Group? Group { get; set; }
    }
}
