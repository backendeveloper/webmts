using AuthService.Data;
using AuthService.Data.Entities;
using AuthService.Data.Repositories;
using AuthService.Test.TestHelpers;
using FluentAssertions;
using Xunit;

namespace AuthService.Test.Integration.Data;

public class UserRepositoryTests : IClassFixture<InMemoryDbContextFixture>
{
    private readonly AuthDbContext _dbContext;
    private readonly UserRepository _repository;

    public UserRepositoryTests(InMemoryDbContextFixture fixture)
    {
        _dbContext = fixture.DbContext;
        _repository = new UserRepository(_dbContext);
    }

    [Fact]
    public async Task GetByUsernameAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var username = "testuser";

        // Act
        var user = await _repository.GetByUsernameAsync(username);

        // Assert
        user.Should().NotBeNull();
        user.Username.Should().Be(username);
    }

    [Fact]
    public async Task GetByUsernameAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var username = "nonexistentuser";

        // Act
        var user = await _repository.GetByUsernameAsync(username);

        // Assert
        user.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var user = await _repository.GetByEmailAsync(email);

        // Assert
        user.Should().NotBeNull();
        user.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetWithRolesAsync_ShouldReturnUserWithRoles()
    {
        // Arrange
        var userId = Guid.Parse("a1b2c3d4-e5f6-7a8b-9c0d-e1f2a3b4c5d6");

        // Act
        var user = await _repository.GetWithRolesAsync(userId);

        // Assert
        user.Should().NotBeNull();
        user.UserRoles.Should().NotBeNull();
        user.UserRoles.Should().HaveCount(1);
        user.UserRoles.First().Role.Name.Should().Be("User");
    }

    [Fact]
    public async Task AddAsync_ShouldAddNewUser()
    {
        // Arrange
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "newuser",
            Email = "new@example.com",
            PasswordHash = "hashedpassword"
        };

        // Act
        await _repository.AddAsync(newUser);
        await _dbContext.SaveChangesAsync();

        // Assert
        var savedUser = await _dbContext.Users.FindAsync(newUser.Id);
        savedUser.Should().NotBeNull();
        savedUser.Username.Should().Be(newUser.Username);
    }

    [Fact]
    public async Task AnyAsync_ShouldReturnTrue_WhenUserExists()
    {
        // Arrange
        var username = "testuser";

        // Act
        var exists = await _repository.AnyAsync(u => u.Username == username);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Arrange
        var username = "nonexistentuser";

        // Act
        var exists = await _repository.AnyAsync(u => u.Username == username);

        // Assert
        exists.Should().BeFalse();
    }
}