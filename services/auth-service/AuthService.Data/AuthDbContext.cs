using AuthService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public sealed class AuthDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
        Database.Migrate();
        SeedInitialData();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.HasOne(e => e.User)
                .WithMany(e => e.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(e => e.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired();
            entity.HasIndex(e => e.Token);

            entity.HasOne(e => e.User)
                .WithMany(e => e.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin", CreatedAt = new DateTime(2024, 03, 17, 0, 0, 0, DateTimeKind.Utc) },
            new Role { Id = 2, Name = "User", CreatedAt = new DateTime(2024, 03, 17, 0, 0, 0, DateTimeKind.Utc) }
        );
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();

        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();

        return base.SaveChanges();
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e is { Entity: BaseEntity, State: EntityState.Added or EntityState.Modified });

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    ((BaseEntity)entry.Entity).CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    ((BaseEntity)entry.Entity).UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void SeedInitialData()
    {
        if (Users.Any(u => u.Username == "admin")) return;
        {
            var adminUser = new User
            {
                Id = new Guid("4a08b65f-24e2-4bb6-b2da-f83d4a4f7970"),
                Username = "admin",
                Email = "admin@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                CreatedAt = new DateTime(2024, 03, 17, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 03, 17, 0, 0, 0, DateTimeKind.Utc)
            };

            if (Users.Any(u => u.Username == "admin")) return;
            Users.Add(adminUser);

            var adminRole = Roles.FirstOrDefault(r => r.Name == "Admin");
            if (adminRole != null)
                UserRoles.Add(new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id,
                    CreatedAt = new DateTime(2024, 03, 17, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 03, 17, 0, 0, 0, DateTimeKind.Utc)
                });

            SaveChanges();
        }
    }
}