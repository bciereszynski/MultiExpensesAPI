using Microsoft.EntityFrameworkCore;
using MultiExpensesAPI.Models;

namespace MultiExpensesAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    public DbSet<User> Users { get; set; }

    public DbSet<Transaction> Transactions { get; set; }

    public DbSet<Group> Groups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>()
            .HasMany(u => u.Groups)
            .WithMany(g => g.Members)
            .UsingEntity(j => j.ToTable("UserGroups"));

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Group)
            .WithMany(g => g.Transactions)
            .HasForeignKey(t => t.GroupId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
