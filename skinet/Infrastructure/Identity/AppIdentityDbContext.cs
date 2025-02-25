using Core.Entities.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

public class AppIdentityDbContext : IdentityDbContext<AppUser>
{
    public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Address> Addresses { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Address>()
            .HasOne(a => a.AppUser)
            .WithOne(u => u.Address)
            .HasForeignKey<Address>(a => a.AppUserId);

        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.AppUser)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.AppUserId);
    }
}