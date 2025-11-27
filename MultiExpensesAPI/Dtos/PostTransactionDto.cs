namespace MultiExpensesAPI.Dtos;

public record PostTransactionDto(string Type, double Amount, string Category, string? Description, DateTime CreatedAt);