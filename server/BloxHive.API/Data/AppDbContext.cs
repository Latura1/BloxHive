using Microsoft.EntityFrameworkCore;
using BloxHive.API.Models;

namespace BloxHive.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<LicenseKey> LicenseKeys => Set<LicenseKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasOne(u => u.LicenseKey).WithOne(lk => lk.UsedByUser).HasForeignKey<User>(u => u.LicenseKeyId);
        });

        modelBuilder.Entity<LicenseKey>(e =>
        {
            e.HasIndex(lk => lk.Key).IsUnique();
        });
    }
}
