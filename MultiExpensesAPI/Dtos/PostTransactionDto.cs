using MultiExpensesAPI.Models.Base;

namespace MultiExpensesAPI.Dtos
{
    public class PostTransactionDto
    {
        public required string Type { get; set; }

        public double Amount { get; set; }

        public required string Category { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
    }

}
