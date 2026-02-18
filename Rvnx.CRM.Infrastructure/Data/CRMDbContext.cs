using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using System.Reflection;

namespace Rvnx.CRM.Infrastructure.Data;

public class CRMDbContext(DbContextOptions<CRMDbContext> options, ICurrentUserService currentUserService) : DbContext(options)
{
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Employer> Employers { get; set; }
    public DbSet<PhoneNumber> PhoneNumbers { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<AttachmentContent> AttachmentContents { get; set; }
    public DbSet<SignificantDate> SignificantDates { get; set; }
    public DbSet<Relationship> Relationships { get; set; }
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<Pet> Pets { get; set; }
    public DbSet<ContactMethod> ContactMethods { get; set; }
    public DbSet<Fact> Facts { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Attachment>()
            .HasOne(a => a.AttachmentContent)
            .WithOne(ac => ac.Attachment)
            .HasForeignKey<AttachmentContent>(ac => ac.AttachmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne(u => u.SelfContact)
            .WithOne()
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Note>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Pet>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Reminder>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<SignificantDate>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<ContactMethod>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Fact>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Address>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Attachment>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<PhoneNumber>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Relationship>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Relationship>().HasIndex(e => new { e.RelatedEntityId, e.EntityType });

        IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType> entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType));

        foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType? entityType in entityTypes)
        {
            modelBuilder.Entity(entityType.Name).HasIndex(nameof(BaseEntity.UserId));

            if (!typeof(IGlobalEntity).IsAssignableFrom(entityType.ClrType))
            {
                MethodInfo? method = typeof(CRMDbContext)
                    .GetMethod(nameof(ConfigureGlobalFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType.ClrType);

                method?.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private void ConfigureGlobalFilter<T>(ModelBuilder modelBuilder) where T : BaseEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => e.UserId == _currentUserService.UserId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    private void UpdateAuditFields()
    {
        IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<BaseEntity>> entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        string username = GetUsername();
        string? userId = GetUserId();

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<BaseEntity>? entry in entries)
        {
            DateTime now = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = now;
                entry.Entity.LastChangedDate = now;
                entry.Entity.CreatedBy = username;
                entry.Entity.LastChangedBy = username;

                if (string.IsNullOrEmpty(entry.Entity.UserId))
                {
                    entry.Entity.UserId = userId;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastChangedDate = now;
                entry.Entity.LastChangedBy = username;
                entry.Property(nameof(BaseEntity.CreatedDate)).IsModified = false;
                entry.Property(nameof(BaseEntity.CreatedBy)).IsModified = false;
                entry.Property(nameof(BaseEntity.UserId)).IsModified = false;
            }
        }
    }

    private string GetUsername()
    {
        return _currentUserService.UserName ?? "System";
    }

    private string? GetUserId()
    {
        return _currentUserService.UserId;
    }
}
