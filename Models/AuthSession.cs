namespace BloxHive.Models;

public class AuthSession
{
    public string Token { get; set; } = "";
    public string Username { get; set; } = "";
    public int UserId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsValid => !string.IsNullOrEmpty(Token) && IsActive && (!ExpiresAt.HasValue || ExpiresAt > DateTime.UtcNow);
}
