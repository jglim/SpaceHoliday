using Microsoft.EntityFrameworkCore;

namespace SpaceHoliday.Database;

public class SpaceDb : DbContext
{
    public SpaceDb(DbContextOptions<SpaceDb> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>()
            .HasIndex(o => o.ServerUrl, "IX_ServerUrl");
        
        modelBuilder.Entity<Organization>()
            .HasIndex(o => o.ClientId, "IX_ClientId");

        modelBuilder.Entity<Organization>()
            .HasMany(o => o.Users)
            .WithOne(m => m.Organization)
            .OnDelete(DeleteBehavior.ClientCascade);
        
        modelBuilder.Entity<User>()
            .HasOne(p => p.Organization)
            .WithMany(o => o.Users);
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
}