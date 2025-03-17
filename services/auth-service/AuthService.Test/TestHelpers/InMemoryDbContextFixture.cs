using AuthService.Data;
using AuthService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Test.TestHelpers;

public class InMemoryDbContextFixture : IDisposable
{
    public AuthDbContext DbContext { get; }

    public InMemoryDbContextFixture()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: $"AuthDb_{Guid.NewGuid()}")
            .Options;

        DbContext = new AuthDbContext(options);
        SeedDatabase();
    }

    private void SeedDatabase()
    {
        // Seed some test data
        var adminRole = new Role
        {
            Id = 1,
            Name = "Admin",
            CreatedAt = DateTime.UtcNow
        };

        var userRole = new Role
        {
            Id = 2,
            Name = "User",
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Roles.AddRange(adminRole, userRole);

        // Add test user
        var testUser = new User
        {
            Id = Guid.Parse("a1b2c3d4-e5f6-7a8b-9c0d-e1f2a3b4c5d6"),
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(testUser);

        // Add user role
        var userRoleMapping = new UserRole
        {
            UserId = testUser.Id,
            RoleId = userRole.Id,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.UserRoles.Add(userRoleMapping);
        DbContext.SaveChanges();
    }

    public void Dispose()
    {
        DbContext.Dispose();
    }
}