using LicensingServer.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace LicensingServer.Web.Data;

public class LicensingDbContext : DbContext
{
    public LicensingDbContext(DbContextOptions<LicensingDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<LicenseActivation> Activations => Set<LicenseActivation>();
    public DbSet<LicenseAuditLog> AuditLogs => Set<LicenseAuditLog>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Fido2Credential> Fido2Credentials => Set<Fido2Credential>();
    public DbSet<AdminLoginAuditLog> AdminLoginAuditLogs => Set<AdminLoginAuditLog>();
    public DbSet<Release> Releases => Set<Release>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LicensingDbContext).Assembly);
    }
}
