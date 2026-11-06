using LibraryBookLendingSystem.Models.Entities;
using LibraryBookLendingSystem.Security;
using Microsoft.EntityFrameworkCore;

namespace LibraryBookLendingSystem.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(LibraryDbContext db)
    {
        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new User { Username = "librarian", FullName = "School Librarian", Role = Roles.Librarian, PasswordHash = PasswordHelper.Hash("Librarian@123") },
                new User { Username = "lrcoordinator", FullName = "LR Coordinator", Role = Roles.LRCoordinator, PasswordHash = PasswordHelper.Hash("Coordinator@123") },
                new User { Username = "admin", FullName = "School Administrator", Role = Roles.Administrator, PasswordHash = PasswordHelper.Hash("Admin@123") });
        }

        if (!await db.Books.AnyAsync())
        {
            db.Books.AddRange(
                NewBook("Noli Me Tangere", "Jose Rizal", "Literature", 2015, 3, "A-1"),
                NewBook("El Filibusterismo", "Jose Rizal", "Literature", 2015, 2, "A-1"),
                NewBook("Biology: Exploring Life", "A. Reyes", "Science", 2010, 2, "B-2"),
                NewBook("Elementary Algebra", "M. Santos", "Mathematics", 2022, 4, "C-1"),
                NewBook("Philippine History", "C. Dela Cruz", "History", 2019, 2, "D-3"),
                NewBook("World Atlas", "National Geographic", "Reference", 2008, 2, "R-1", damaged: 1, note: "Torn binding"));
        }

        if (!await db.Members.AnyAsync())
        {
            db.Members.AddRange(
                new Member { LibraryCardNo = "MBASH-0001", FullName = "Juan Dela Cruz", GradeSection = "Grade 8 - Sampaguita", ContactNumber = "09170000001" },
                new Member { LibraryCardNo = "MBASH-0002", FullName = "Maria Santos", GradeSection = "Grade 9 - Rosal", ContactNumber = "09170000002" },
                new Member { LibraryCardNo = "MBASH-0003", FullName = "Pedro Reyes", GradeSection = "Grade 10 - Orchid", ContactNumber = "09170000003" });
        }

        await db.SaveChangesAsync();
    }

    private static Book NewBook(string title, string author, string category, int year, int copies, string shelf,
        int damaged = 0, string note = "") => new()
    {
        Title = title, Author = author, Category = category, PublicationYear = year, ShelfLocation = shelf,
        TotalCopies = copies, DamagedCopies = damaged, AvailableCopies = copies - damaged, ConditionNote = note
    };
}
