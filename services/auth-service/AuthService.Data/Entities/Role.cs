namespace AuthService.Data.Entities;

public class Role : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}