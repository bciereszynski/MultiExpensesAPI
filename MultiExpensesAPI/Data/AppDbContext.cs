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

    public DbSet<GroupInvitation> GroupInvitations { get; set; }

    public DbSet<TransactionSplit> TransactionSplits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasMany(u => u.Groups)
            .WithMany(g => g.Members)
            .UsingEntity(j => j.ToTable("UserGroups"));

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Group)
            .WithMany(g => g.Transactions)
            .HasForeignKey(t => t.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GroupInvitation>()
            .HasIndex(gi => gi.Token)
            .IsUnique();

        modelBuilder.Entity<TransactionSplit>()
            .HasOne(ts => ts.Transaction)
            .WithMany(t => t.Splits)
            .HasForeignKey(ts => ts.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TransactionSplit>()
            .HasOne(ts => ts.User)
            .WithMany()
            .HasForeignKey(ts => ts.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
