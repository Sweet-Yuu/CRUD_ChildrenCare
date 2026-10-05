using CRUD_ChildrenCare.Models;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<RoleMenu> RoleMenus => Set<RoleMenu>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var setting = modelBuilder.Entity<Setting>();
        setting.Property(item => item.Type).HasConversion<string>().HasMaxLength(50);
        setting.Property(item => item.Name).HasMaxLength(100);
        setting.Property(item => item.Value).HasMaxLength(255);
        setting.Property(item => item.Description).HasMaxLength(500);
        setting.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            setting.Property(item => item.Name).UseCollation("NOCASE");
        }

        setting.HasIndex(item => new { item.Type, item.Name }).IsUnique();

        var user = modelBuilder.Entity<User>();
        user.Property(item => item.FullName).HasMaxLength(100);
        user.Property(item => item.Gender).HasConversion<string>().HasMaxLength(20);
        user.Property(item => item.Email).HasMaxLength(100);
        user.Property(item => item.NormalizedEmail).HasMaxLength(100);
        user.Property(item => item.Mobile).HasMaxLength(10).IsUnicode(false);
        user.Property(item => item.Address).HasMaxLength(255);
        user.Property(item => item.AvatarUrl).HasMaxLength(255);
        user.Property(item => item.PasswordHash).HasMaxLength(255);
        user.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        user.Property(item => item.VerifyTokenHash).HasMaxLength(100);
        user.Property(item => item.ResetTokenHash).HasMaxLength(100);
        user.HasIndex(item => item.NormalizedEmail).IsUnique();
        user.HasOne(item => item.Role)
            .WithMany(item => item.Users)
            .HasForeignKey(item => item.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        var roleMenu = modelBuilder.Entity<RoleMenu>();
        roleMenu.HasKey(item => new { item.RoleId, item.MenuId });
        roleMenu.HasOne(item => item.Role)
            .WithMany(item => item.RoleMenuRoles)
            .HasForeignKey(item => item.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        roleMenu.HasOne(item => item.Menu)
            .WithMany(item => item.RoleMenuEntries)
            .HasForeignKey(item => item.MenuId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<User>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = now;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedDate = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
