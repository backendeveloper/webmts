namespace AuthService.Data.Entities;

public class User : BaseEntity
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    public List<UserRole> UserRoles { get; set; } = [];
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}