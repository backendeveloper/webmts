namespace AuthService.Data.Entities;

public class RefreshToken : BaseEntity
{
    public Guid Id { get; set; }
    public string Token { get; set; }
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    
    public virtual User User { get; set; }
}