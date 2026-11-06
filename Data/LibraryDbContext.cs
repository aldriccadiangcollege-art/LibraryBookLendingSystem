using LibraryBookLendingSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryBookLendingSystem.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>(e =>
        {
            e.Property(x => x.Username).HasMaxLength(50);
            e.Property(x => x.FullName).HasMaxLength(100);
            e.Property(x => x.Role).HasMaxLength(30);
            e.HasIndex(x => x.Username).IsUnique();
        });

        mb.Entity<Book>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Author).HasMaxLength(150);
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.ShelfLocation).HasMaxLength(50);
            e.Property(x => x.ConditionNote).HasMaxLength(200);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.WithdrawnReason).HasMaxLength(30);
            e.HasIndex(x => new { x.Title, x.Author });
        });

        mb.Entity<Member>(e =>
        {
            e.Property(x => x.LibraryCardNo).HasMaxLength(50);
            e.Property(x => x.FullName).HasMaxLength(150);
            e.Property(x => x.GradeSection).HasMaxLength(50);
            e.Property(x => x.ContactNumber).HasMaxLength(30);
            e.HasIndex(x => x.LibraryCardNo).IsUnique();
        });

        mb.Entity<Loan>(e =>
        {
            e.Property(x => x.FineAmount).HasPrecision(10, 2);
            e.Property(x => x.ReturnCondition).HasMaxLength(20);
            e.HasOne(x => x.Book).WithMany(b => b.Loans).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Member).WithMany(m => m.Loans).HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<AuditLog>(e =>
        {
            e.Property(x => x.Username).HasMaxLength(50);
            e.Property(x => x.Action).HasMaxLength(50);
            e.Property(x => x.Details).HasMaxLength(500);
            e.HasIndex(x => x.Timestamp);
        });
    }
}
