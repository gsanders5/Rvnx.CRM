using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Activity;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using System.Reflection;

namespace Rvnx.CRM.Infrastructure.Data;

public class CRMDbContext(DbContextOptions<CRMDbContext> options, ICurrentUserService currentUserService) : DbContext(options)
{
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public DbSet<Contact>? Contacts { get; set; }
    public DbSet<Employer>? Employers { get; set; }
    public DbSet<Note>? Notes { get; set; }
    public DbSet<Attachment>? Attachments { get; set; }
    public DbSet<AttachmentContent>? AttachmentContents { get; set; }
    public DbSet<SignificantDate>? SignificantDates { get; set; }
    public DbSet<ReminderOffset>? ReminderOffsets { get; set; }
    public DbSet<ReminderLog>? ReminderLogs { get; set; }
    public DbSet<Relationship>? Relationships { get; set; }
    public DbSet<Pet>? Pets { get; set; }
    public DbSet<PetContact>? PetContacts { get; set; }
    public DbSet<ContactMethod>? ContactMethods { get; set; }
    public DbSet<Fact>? Facts { get; set; }
    public DbSet<Address>? Addresses { get; set; }
    public DbSet<User>? Users { get; set; }
    public DbSet<UserGroup>? UserGroups { get; set; }
    public DbSet<Label>? Labels { get; set; }
    public DbSet<ContactLabel>? ContactLabels { get; set; }
    public DbSet<ContactFavorite>? ContactFavorites { get; set; }
    public DbSet<Activity>? Activities { get; set; }
    public DbSet<ActivityContact>? ActivityContacts { get; set; }
    public DbSet<ContactTask>? ContactTasks { get; set; }
    public DbSet<ApiToken>? ApiTokens { get; set; }

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

        modelBuilder.Entity<User>()
            .HasIndex(u => u.SelfContactId)
            .IsUnique();

        modelBuilder.Entity<Relationship>()
            .Property(e => e.EntityType)
            .HasConversion<string>()
            .HasMaxLength(50);

        modelBuilder.Entity<Relationship>().HasIndex(e => new { e.EntityId, e.EntityType });
        modelBuilder.Entity<Relationship>().HasIndex(e => new { e.RelatedEntityId, e.EntityType });

        modelBuilder.Entity<PetContact>()
            .HasOne(pc => pc.Pet)
            .WithMany(p => p.PetContacts)
            .HasForeignKey(pc => pc.PetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PetContact>()
            .HasOne(pc => pc.Contact)
            .WithMany(c => c.PetContacts)
            .HasForeignKey(pc => pc.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PetContact>()
            .HasIndex(pc => new { pc.PetId, pc.ContactId })
            .IsUnique();

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

        modelBuilder.Entity<ContactFavorite>()
            .HasOne(cf => cf.Contact)
            .WithMany()
            .HasForeignKey(cf => cf.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        // A user can only favorite a given contact once; UserId+ContactId must be unique per group
        modelBuilder.Entity<ContactFavorite>()
            .HasIndex(cf => new { cf.UserId, cf.ContactId })
            .IsUnique();

        modelBuilder.Entity<ActivityContact>()
            .HasOne(ac => ac.Activity)
            .WithMany(a => a.ActivityContacts)
            .HasForeignKey(ac => ac.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivityContact>()
            .HasOne(ac => ac.Contact)
            .WithMany(c => c.ActivityContacts)
            .HasForeignKey(ac => ac.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivityContact>()
            .HasIndex(ac => new { ac.ActivityId, ac.ContactId })
            .IsUnique();

        modelBuilder.Entity<ContactMethod>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.ContactMethods)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ContactMethod>()
            .ToTable(t => t.HasCheckConstraint("CHK_ContactMethod_Owner", "ContactId IS NOT NULL"));

        modelBuilder.Entity<Fact>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.Facts)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Fact>()
            .ToTable(t => t.HasCheckConstraint("CHK_Fact_Owner", "ContactId IS NOT NULL"));

        modelBuilder.Entity<Address>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.Addresses)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Address>()
            .ToTable(t => t.HasCheckConstraint("CHK_Address_Owner", "ContactId IS NOT NULL"));

        modelBuilder.Entity<ContactTask>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.ContactTasks)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ContactTask>()
            .ToTable(t => t.HasCheckConstraint("CHK_ContactTask_Owner", "ContactId IS NOT NULL"));

        modelBuilder.Entity<Note>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.Notes)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Note>()
            .ToTable(t => t.HasCheckConstraint("CHK_Note_Owner", "ContactId IS NOT NULL"));

        modelBuilder.Entity<SignificantDate>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.SignificantDates)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SignificantDate>()
            .ToTable(t => t.HasCheckConstraint("CHK_SignificantDate_Owner", "ContactId IS NOT NULL"));

        modelBuilder.Entity<ReminderOffset>()
            .HasOne(ro => ro.SignificantDate)
            .WithMany(sd => sd.ReminderOffsets)
            .HasForeignKey(ro => ro.SignificantDateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReminderLog>()
            .HasOne(rl => rl.ReminderOffset)
            .WithMany(ro => ro.ReminderLogs)
            .HasForeignKey(rl => rl.ReminderOffsetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReminderLog>()
            .HasIndex(rl => new { rl.ReminderOffsetId, rl.OccurrenceDate })
            .IsUnique();

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
            modelBuilder.Entity(entityType.Name).HasIndex(nameof(BaseEntity.GroupId));

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
        modelBuilder.Entity<T>().HasQueryFilter(e => e.GroupId == _currentUserService.GroupId);
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
        Guid? groupId = GetGroupId();

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

                if (entry.Entity.GroupId == null)
                {
                    entry.Entity.GroupId = groupId;
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

    private Guid? GetGroupId()
    {
        return _currentUserService.GroupId;
    }
}