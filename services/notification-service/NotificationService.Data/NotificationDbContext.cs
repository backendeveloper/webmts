using Microsoft.EntityFrameworkCore;
using NotificationService.Contract.Enums;
using NotificationService.Data.Entities;

namespace NotificationService.Data;

public sealed class NotificationDbContext : DbContext
{
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
        Database.Migrate();
        SeedInitialData();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Subject)
                .HasMaxLength(200);

            entity.Property(e => e.Content)
                .HasMaxLength(4000);

            entity.Property(e => e.TemplateId)
                .HasMaxLength(100);

            entity.Property(e => e.RecipientId)
                .HasMaxLength(100);

            entity.Property(e => e.RecipientInfo)
                .HasMaxLength(200);

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(500);

            entity.Property(e => e.RelatedEntityId)
                .HasMaxLength(100);

            entity.Property(e => e.RelatedEntityType)
                .HasMaxLength(100);
        });

        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.ToTable("NotificationTemplates");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.SubjectTemplate)
                .HasMaxLength(200);

            entity.Property(e => e.BodyTemplate)
                .HasMaxLength(4000);
        });
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
        var notificationTemplates = new List<NotificationTemplate>
        {
            new NotificationTemplate
            {
                Id = Guid.Parse("16b49da3-6b90-4d73-ab5c-5e927c3b3e32"),
                Name = "TransactionCreated",
                Description = "İşlem oluşturulduğunda gönderilen bildirim",
                Type = NotificationType.Email,
                SubjectTemplate = "WEBMTS - İşleminiz Oluşturuldu",
                BodyTemplate =
                    "Sayın {{CustomerName}}, {{Amount}} {{Currency}} tutarındaki işleminiz başarıyla oluşturulmuştur. İşlem numaranız: {{TransactionNumber}}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("8e9a3c79-c2a0-44d1-8e5f-909c1d5bef61"),
                Name = "TransactionCompleted",
                Description = "İşlem tamamlandığında gönderilen bildirim",
                Type = NotificationType.Email,
                SubjectTemplate = "WEBMTS - İşleminiz Tamamlandı",
                BodyTemplate =
                    "Sayın {{CustomerName}}, {{Amount}} {{Currency}} tutarındaki işleminiz başarıyla tamamlanmıştır. İşlem numaranız: {{TransactionNumber}}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("a03eed6b-e7e5-49eb-9a96-2a757ddd279f"),
                Name = "TransactionFailed",
                Description = "İşlem başarısız olduğunda gönderilen bildirim",
                Type = NotificationType.Email,
                SubjectTemplate = "WEBMTS - İşleminiz Başarısız Oldu",
                BodyTemplate =
                    "Sayın {{CustomerName}}, {{Amount}} {{Currency}} tutarındaki işleminiz tamamlanamadı. Lütfen müşteri hizmetleri ile iletişime geçin. İşlem numaranız: {{TransactionNumber}}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("c3542ea3-46b7-4e88-a9e4-03d34c5c54c1"),
                Name = "NewCustomer",
                Description = "Yeni müşteri kaydı oluşturulduğunda gönderilen bildirim",
                Type = NotificationType.Email,
                SubjectTemplate = "WEBMTS - Hoş Geldiniz",
                BodyTemplate = "Sayın {{CustomerName}}, WEBMTS'ye hoş geldiniz. Hesabınız başarıyla oluşturulmuştur.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        NotificationTemplates.AddRange(notificationTemplates);
    }
}