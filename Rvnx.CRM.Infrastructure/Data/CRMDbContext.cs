using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Infrastructure.Data;

public class CRMDbContext(DbContextOptions<CRMDbContext> options) : DbContext(options)
{
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Attachment 1:1 with Content
        modelBuilder.Entity<Attachment>()
            .HasOne(a => a.AttachmentContent)
            .WithOne(ac => ac.Attachment)
            .HasForeignKey<AttachmentContent>(ac => ac.AttachmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add indices for polymorphic lookups
        modelBuilder.Entity<Note>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Pet>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Reminder>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<ImportantDate>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Attachment>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<PhoneNumber>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Relationship>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Relationship>().HasIndex(e => new { e.RelatedEntityId, e.EntityType });

        // Seed RelationshipTypes
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<RelationshipType>().HasData(
            new RelationshipType { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Parent", OppositeName = "Child", EntityType = EntityTypes.Person, CreatedBy = "System", LastChangedBy = "System", CreatedDate = seedDate, LastChangedDate = seedDate },
            new RelationshipType { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Spouse", OppositeName = "Spouse", EntityType = EntityTypes.Person, CreatedBy = "System", LastChangedBy = "System", CreatedDate = seedDate, LastChangedDate = seedDate },
            new RelationshipType { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Sibling", OppositeName = "Sibling", EntityType = EntityTypes.Person, CreatedBy = "System", LastChangedBy = "System", CreatedDate = seedDate, LastChangedDate = seedDate },
            new RelationshipType { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Friend", OppositeName = "Friend", EntityType = EntityTypes.Person, CreatedBy = "System", LastChangedBy = "System", CreatedDate = seedDate, LastChangedDate = seedDate },
            new RelationshipType { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Partner", OppositeName = "Partner", EntityType = EntityTypes.Person, CreatedBy = "System", LastChangedBy = "System", CreatedDate = seedDate, LastChangedDate = seedDate },
            new RelationshipType { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Manager", OppositeName = "Employee", EntityType = EntityTypes.Person, CreatedBy = "System", LastChangedBy = "System", CreatedDate = seedDate, LastChangedDate = seedDate },
            new RelationshipType { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), Name = "Teacher", OppositeName = "Student", EntityType = EntityTypes.Person, CreatedBy = "System", LastChangedBy = "System", CreatedDate = seedDate, LastChangedDate = seedDate },
            new RelationshipType { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), Name = "Parent Company", OppositeName = "Subsidiary", EntityType = EntityTypes.Company, CreatedBy = "System", LastChangedBy = "System", CreatedDate = seedDate, LastChangedDate = seedDate }
        );
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
        var entries = ChangeTracker.Entries<CRMBaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var now = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = now;
                entry.Entity.LastChangedDate = now;
                entry.Entity.CreatedBy = GetUsername();
                entry.Entity.LastChangedBy = GetUsername();
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastChangedDate = now;
                entry.Entity.LastChangedBy = GetUsername();
                entry.Property(nameof(CRMBaseEntity.CreatedDate)).IsModified = false;
                entry.Property(nameof(CRMBaseEntity.CreatedBy)).IsModified = false;
            }
        }
    }

    private static string GetUsername()
    {
        // Placeholder - Will update when OAuth configured.
        var username = Environment.UserName;
        return username ?? string.Empty;
    }
}
