namespace MultiExpensesAPI.Dtos;

public record CreateInvitationResponseDto(string InvitationToken, DateTime ExpiresAt);

public record AcceptInvitationDto(string Token);

public record InvitationDto(int Id, string Token, DateTime CreatedAt, DateTime ExpiresAt);