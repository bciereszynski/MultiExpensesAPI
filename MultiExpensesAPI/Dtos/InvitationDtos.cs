namespace MultiExpensesAPI.Dtos;

public record CreateInvitationResponseDto(string InvitationToken, DateTime ExpiresAt);

public record AcceptInvitationDto(string Token);