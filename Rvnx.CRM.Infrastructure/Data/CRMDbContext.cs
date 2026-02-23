using Microsoft.EntityFrameworkCore;
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
    public DbSet<Label> Labels { get; set; }
    public DbSet<ContactLabel> ContactLabels { get; set; }

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

        modelBuilder.Entity<Relationship>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Relationship>().HasIndex(e => new { e.RelatedEntityId, e.EntityType });

        // Pet - Required FK
        modelBuilder.Entity<Pet>()
            .HasOne(p => p.Contact)
            .WithMany(c => c.Pets)
            .HasForeignKey(p => p.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        // ContactLabel
        modelBuilder.Entity<ContactLabel>()
            .HasOne(cl => cl.Contact)
            .WithMany(c => c.ContactLabels)
            .HasForeignKey(cl => cl.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ContactLabel>()
            .HasOne(cl => cl.Label)
            .WithMany(l => l.ContactLabels)
            .HasForeignKey(cl => cl.LabelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ContactLabel>().HasIndex(cl => new { cl.ContactId, cl.LabelId }).IsUnique();

        // ContactMethod
        modelBuilder.Entity<ContactMethod>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.ContactMethods)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ContactMethod>()
            .ToTable(t => t.HasCheckConstraint("CHK_ContactMethod_Owner", "ContactId IS NOT NULL"));

        // Fact
        modelBuilder.Entity<Fact>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.Facts)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Fact>()
            .ToTable(t => t.HasCheckConstraint("CHK_Fact_Owner", "ContactId IS NOT NULL"));

        // Address
        modelBuilder.Entity<Address>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.Addresses)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Address>()
            .ToTable(t => t.HasCheckConstraint("CHK_Address_Owner", "ContactId IS NOT NULL"));

        // PhoneNumber
        modelBuilder.Entity<PhoneNumber>()
            .HasOne(e => e.Contact)
            .WithMany()
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PhoneNumber>()
            .ToTable(t => t.HasCheckConstraint("CHK_PhoneNumber_Owner", "ContactId IS NOT NULL"));

        // Note
        modelBuilder.Entity<Note>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.Notes)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Note>()
            .ToTable(t => t.HasCheckConstraint("CHK_Note_Owner", "ContactId IS NOT NULL"));

        // Reminder
        modelBuilder.Entity<Reminder>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.Reminders)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Reminder>()
            .ToTable(t => t.HasCheckConstraint("CHK_Reminder_Owner", "ContactId IS NOT NULL"));

        // SignificantDate
        modelBuilder.Entity<SignificantDate>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.SignificantDates)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SignificantDate>()
            .ToTable(t => t.HasCheckConstraint("CHK_SignificantDate_Owner", "ContactId IS NOT NULL"));

        // Attachment
        modelBuilder.Entity<Attachment>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.Attachments)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Attachment>()
            .ToTable(t => t.HasCheckConstraint("CHK_Attachment_Owner", "ContactId IS NOT NULL"));

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
        Guid? userId = GetUserId();

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<BaseEntity>? entry in entries)
        {
            DateTime now = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = now;
                entry.Entity.LastChangedDate = now;
                entry.Entity.CreatedBy = username;
                entry.Entity.LastChangedBy = username;

                if (entry.Entity.UserId == null)
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
            }
        }
    }

    private string GetUsername()
    {
        return _currentUserService.UserName ?? "System";
    }

    private Guid? GetUserId()
    {
        return _currentUserService.UserId;
    }
}