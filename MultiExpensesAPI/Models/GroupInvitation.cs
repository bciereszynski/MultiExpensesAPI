using MultiExpensesAPI.Models.Base;

namespace MultiExpensesAPI.Models;

public class GroupInvitation : Entity
{
    public int GroupId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public Group Group { get; set; } = null!;
}