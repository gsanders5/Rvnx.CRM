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
    public DbSet<ImportantDate> ImportantDates { get; set; }
    public DbSet<Relationship> Relationships { get; set; }
    public DbSet<RelationshipType> RelationshipTypes { get; set; }
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<Pet> Pets { get; set; }
    public DbSet<ContactInfo> ContactInfos { get; set; }
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

        modelBuilder.Entity<Note>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Pet>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Reminder>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<ImportantDate>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<ContactInfo>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Fact>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Address>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Attachment>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<PhoneNumber>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Relationship>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Relationship>().HasIndex(e => new { e.RelatedEntityId, e.EntityType });

        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(CRMBaseEntity).IsAssignableFrom(e.ClrType));

        foreach (var entityType in entityTypes)
        {
            modelBuilder.Entity(entityType.Name).HasIndex(nameof(CRMBaseEntity.UserId));

            if (!typeof(IGlobalEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(CRMDbContext)
                    .GetMethod(nameof(ConfigureGlobalFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType.ClrType);

                method?.Invoke(this, new object[] { modelBuilder });
            }
        }

        DateTime seedDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<RelationshipType>().HasData(
            new RelationshipType { Id = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a"), Name = "Parent", OppositeName = "Child", EntityType = EntityTypes.Person, CreatedBy = "System", LastChangedBy = "System", CreatedDate = seedDate, LastChangedDate = seedDate },
            new RelationshipType { Id = Guid.Parse("b2e9a5c8-7f4d-4a1b-8c6e-5f9d3a0e2b4c"), Name = "Spouse", OppositeName = "Spouse", EntityType = EntityTypes.Person, CreatedBy = "System", LastChangedBy = "System", CreatedDate = seedDate, LastChangedDate = seedDate },
            new RelationshipType { Id = Guid.Parse("d4f1b8a9-3e2c-4b5d-9a6f-1c0e7d8b5a2f"), Name = "Sibling", OppositeName = "Sibling", EntityType = EntityTypes.Person, CreatedBy = "System", LastChangedBy = "System", CreatedDate = seedDate, LastChangedDate = seedDate },
            new RelationshipType { Id = Guid.Parse("a5b6c7d8-9e0f-1a2b-3c4d-5e6f7a8b9c0d"), Name = "Friend", OppositeName = "Friend", EntityType = EntityTypes.Person, CreatedBy = "System", LastChangedBy = "System", CreatedDate = seedDate, LastChangedDate = seedDate },
            new RelationshipType { Id = Guid.Parse("f9e8d7c6-b5a4-3210-9876-543210fedcba"), Name = "Partner", OppositeName = "Partner", EntityType = EntityTypes.Person, CreatedBy = "System", LastChangedBy = "System", CreatedDate = seedDate, LastChangedDate = seedDate },
            new RelationshipType { Id = Guid.Parse("1a2b3c4d-5e6f-7890-a1b2-c3d4e5f67890"), Name = "Manager", OppositeName = "Employee", EntityType = EntityTypes.Person, CreatedBy = "System", LastChangedBy = "System", CreatedDate = seedDate, LastChangedDate = seedDate },
            new RelationshipType { Id = Guid.Parse("09876543-210f-edcb-a987-6543210fedcb"), Name = "Teacher", OppositeName = "Student", EntityType = EntityTypes.Person, CreatedBy = "System", LastChangedBy = "System", CreatedDate = seedDate, LastChangedDate = seedDate },
            new RelationshipType { Id = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210"), Name = "Parent Company", OppositeName = "Subsidiary", EntityType = EntityTypes.Company, CreatedBy = "System", LastChangedBy = "System", CreatedDate = seedDate, LastChangedDate = seedDate }
        );
    }

    private void ConfigureGlobalFilter<T>(ModelBuilder modelBuilder) where T : CRMBaseEntity
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
        IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<CRMBaseEntity>> entries = ChangeTracker.Entries<CRMBaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        string username = GetUsername();
        string? userId = GetUserId();

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<CRMBaseEntity>? entry in entries)
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
                entry.Property(nameof(CRMBaseEntity.CreatedDate)).IsModified = false;
                entry.Property(nameof(CRMBaseEntity.CreatedBy)).IsModified = false;
                entry.Property(nameof(CRMBaseEntity.UserId)).IsModified = false;
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
