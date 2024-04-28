using Microsoft.EntityFrameworkCore;
using Online_API.Models;

public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    
    public DbSet<AchievementsUsers> AchievementsUsers { get; set; }
    
    public DbSet<Achievement> Achievements { get; set; }
    
    public DbSet<Role> Roles { get; set; }
    
    public DbSet<Rank> Ranks { get; set; }
    
    public DbSet<UsersRanks> UsersRanks { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<AchievementsUsers>()
            .HasKey(ua => new { ua.UserId, ua.AchievementId }); // Composite primary key
        
        modelBuilder.Entity<UsersRanks>()
            .HasKey(ur => new { ur.UserId}); // Composite primary key
    }
}