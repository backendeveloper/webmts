using AuthService.Business.Models;

namespace AuthService.Business.Services;

public class InMemoryUserStore
{
    private readonly List<UserModel> _users = new();

    public InMemoryUserStore()
    {
        // Başlangıç için bir demo kullanıcısı ekleyelim
        _users.Add(new UserModel
        {
            Id = Guid.Parse("8a7d8f16-9e6c-4e7b-b5a1-2c123d456789"),
            Username = "admin",
            Email = "admin@example.com",
            // "password" için BCrypt hash 
            PasswordHash = "$2a$12$NSjP3cJbUyZ/vIyDzKWXvO5wjrKXRTJSJUWWaWZ.a1LiLkQ9CHG6m",
            Roles = new List<string> { "Admin" },
            CreatedAt = DateTime.UtcNow
        });
    }

    public UserModel FindByUsername(string username)
    {
        return _users.FirstOrDefault(u => u.Username == username);
    }

    public UserModel FindByEmail(string email)
    {
        return _users.FirstOrDefault(u => u.Email == email);
    }

    public UserModel FindById(Guid id)
    {
        return _users.FirstOrDefault(u => u.Id == id);
    }

    public void Add(UserModel user)
    {
        _users.Add(user);
    }

    public void UpdateLastLogin(Guid userId, DateTime loginTime)
    {
        var user = FindById(userId);
        if (user != null)
        {
            user.LastLoginAt = loginTime;
        }
    }
}