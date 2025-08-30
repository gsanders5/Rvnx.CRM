using Microsoft.EntityFrameworkCore;
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
    public DbSet<ImportantDate> ImportantDates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
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