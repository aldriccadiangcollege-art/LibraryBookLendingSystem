using System.Text;
using LibraryBookLendingSystem.Exceptions;
using LibraryBookLendingSystem.Helpers;
using LibraryBookLendingSystem.Models.DTOs.Books;
using LibraryBookLendingSystem.Models.DTOs.Common;
using LibraryBookLendingSystem.Models.Entities;
using LibraryBookLendingSystem.Repositories.Interfaces;
using LibraryBookLendingSystem.Services.Interfaces;

namespace LibraryBookLendingSystem.Services.Implementations;

public class BookService : IBookService
{
    private readonly IBookRepository _books;
    private readonly IAuditService _audit;

    public BookService(IBookRepository books, IAuditService audit)
    {
        _books = books;
        _audit = audit;
    }

    public static string ConditionOf(Book b) =>
        b.DamagedCopies == 0 ? "Good" : b.DamagedCopies >= b.TotalCopies ? "Damaged" : "Partly damaged";

    public static BookResponse ToResponse(Book b) => new(b.Id, b.Title, b.Author, b.Category, b.PublicationYear, b.ShelfLocation,
        b.TotalCopies, b.AvailableCopies, b.DamagedCopies, ConditionOf(b), b.ConditionNote,
        b.Status, b.WithdrawnReason, DateTime.Now.Year - b.PublicationYear >= Book.OutdatedYears);

    private static int Borrowed(Book b) => b.TotalCopies - b.DamagedCopies - b.AvailableCopies;

    public async Task<List<BookResponse>> GetAllAsync(string? q, string? category, bool availableOnly, bool includeWithdrawn) =>
        (await _books.GetAllAsync(q, category, availableOnly, includeWithdrawn)).Select(ToResponse).ToList();

    public Task<List<string>> CategoriesAsync() => _books.CategoriesAsync();

    public async Task<BookResponse> GetAsync(int id) =>
        ToResponse(await _books.GetAsync(id) ?? throw new NotFoundException($"Book {id} was not found."));

    public async Task<BookResponse> CreateAsync(BookRequest r)
    {
        var title = r.Title.Trim();
        var author = r.Author.Trim();
        if (r.DamagedCopies > r.TotalCopies) throw new ConflictException("Damaged copies cannot be more than the total copies.");
        if (await _books.ExistsAsync(title, author, null))
            throw new ConflictException("This book is already in the catalog. Edit it and increase its total copies instead of adding a duplicate.");

        var book = new Book
        {
            Title = title, Author = author, Category = r.Category.Trim(), PublicationYear = r.PublicationYear,
            ShelfLocation = r.ShelfLocation.Trim(), ConditionNote = r.ConditionNote.Trim(),
            TotalCopies = r.TotalCopies, DamagedCopies = r.DamagedCopies, AvailableCopies = r.TotalCopies - r.DamagedCopies
        };
        _books.Add(book);
        _audit.Record("BookAdded", $"'{title}' by {author}, {r.TotalCopies} copies.");
        await _books.SaveAsync();
        return ToResponse(book);
    }

    public async Task<BookResponse> UpdateAsync(int id, BookRequest r)
    {
        var book = await _books.GetAsync(id) ?? throw new NotFoundException($"Book {id} was not found.");
        var title = r.Title.Trim();
        var author = r.Author.Trim();
        if (await _books.ExistsAsync(title, author, id))
            throw new ConflictException("Another book with the same title and author already exists.");
        if (r.DamagedCopies > r.TotalCopies)
            throw new ConflictException("Damaged copies cannot be more than the total copies.");

        var borrowed = Borrowed(book);
        var available = r.TotalCopies - r.DamagedCopies - borrowed;
        if (available < 0)
            throw new ConflictException($"{borrowed} copy(ies) are currently borrowed, so total minus damaged copies cannot be lower than that.");

        book.Title = title;
        book.Author = author;
        book.Category = r.Category.Trim();
        book.PublicationYear = r.PublicationYear;
        book.ShelfLocation = r.ShelfLocation.Trim();
        book.ConditionNote = r.ConditionNote.Trim();
        book.TotalCopies = r.TotalCopies;
        book.DamagedCopies = r.DamagedCopies;
        book.AvailableCopies = book.Status == "Active" ? available : 0;
        _audit.Record("BookUpdated", $"'{title}' (id {id}).");
        await _books.SaveAsync();
        return ToResponse(book);
    }

    public async Task<BookResponse> WithdrawAsync(int id, WithdrawRequest r)
    {
        var book = await _books.GetAsync(id) ?? throw new NotFoundException($"Book {id} was not found.");
        if (book.Status == "Withdrawn") throw new ConflictException("This book is already withdrawn.");
        var borrowed = Borrowed(book);
        if (borrowed > 0)
            throw new ConflictException($"{borrowed} copy(ies) are still borrowed. Wait until they are returned before withdrawing the book.");

        book.Status = "Withdrawn";
        book.WithdrawnReason = r.Reason;
        book.WithdrawnDate = DateTime.Now;
        book.AvailableCopies = 0;
        _audit.Record("BookWithdrawn", $"'{book.Title}' (id {id}) withdrawn: {r.Reason}.");
        await _books.SaveAsync();
        return ToResponse(book);
    }

    public async Task<BookResponse> RestoreAsync(int id)
    {
        var book = await _books.GetAsync(id) ?? throw new NotFoundException($"Book {id} was not found.");
        if (book.Status == "Active") throw new ConflictException("This book is already active.");

        book.Status = "Active";
        book.WithdrawnReason = null;
        book.WithdrawnDate = null;
        book.AvailableCopies = book.TotalCopies - book.DamagedCopies;
        _audit.Record("BookRestored", $"'{book.Title}' (id {id}) returned to the collection.");
        await _books.SaveAsync();
        return ToResponse(book);
    }

    public async Task DeleteAsync(int id)
    {
        var book = await _books.GetAsync(id) ?? throw new NotFoundException($"Book {id} was not found.");
        if (await _books.HasLoansAsync(id))
            throw new ConflictException("This book has lending records and cannot be deleted. Withdraw it instead.");
        _books.Remove(book);
        _audit.Record("BookDeleted", $"'{book.Title}' (id {id}).");
        await _books.SaveAsync();
    }

    /// <summary>Columns: Title, Author, Category, PublicationYear, Copies, ShelfLocation.</summary>
    public async Task<ImportResult> ImportAsync(string csv)
    {
        var rows = CsvUtil.Parse(csv);
        if (rows.Count > 0 && rows[0][0].Equals("Title", StringComparison.OrdinalIgnoreCase)) rows.RemoveAt(0);

        var added = 0;
        var skipped = 0;
        var messages = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            string Get(int idx) => idx < r.Length ? r[idx] : "";
            var n = i + 1;
            var title = Get(0);
            var author = Get(1);
            var category = Get(2) == "" ? "General" : Get(2);
            var shelf = Get(5);

            if (title == "" || author == "") { skipped++; messages.Add($"Row {n}: title and author are required."); continue; }
            if (title.Length > 200 || author.Length > 150 || category.Length > 100 || shelf.Length > 50)
            { skipped++; messages.Add($"Row {n}: a value is too long."); continue; }
            if (!int.TryParse(Get(3), out var year) || year < 1800 || year > 2100)
            { skipped++; messages.Add($"Row {n}: publication year must be between 1800 and 2100."); continue; }
            if (!int.TryParse(Get(4), out var copies) || copies < 1 || copies > 1000)
            { skipped++; messages.Add($"Row {n}: copies must be a number from 1 to 1000."); continue; }
            if (!seen.Add(title + "|" + author) || await _books.ExistsAsync(title, author, null))
            { skipped++; messages.Add($"Row {n}: '{title}' by {author} is already in the catalog."); continue; }

            _books.Add(new Book
            {
                Title = title, Author = author, Category = category, PublicationYear = year,
                ShelfLocation = shelf, TotalCopies = copies, AvailableCopies = copies
            });
            added++;
        }

        if (added > 0)
        {
            _audit.Record("BookImport", $"{added} added, {skipped} skipped.");
            await _books.SaveAsync();
        }
        return new ImportResult(added, skipped, messages);
    }

    /// <summary>Inventory list that the school can use when encoding or reporting to the division or LRMIS.</summary>
    public async Task<string> ExportCsvAsync()
    {
        var sb = new StringBuilder("Id,Title,Author,Category,PublicationYear,ShelfLocation,TotalCopies,AvailableCopies,DamagedCopies,Status,WithdrawnReason\n");
        foreach (var b in await _books.GetAllAsync(null, null, false, true))
        {
            sb.Append(string.Join(',', new[]
            {
                b.Id.ToString(), CsvUtil.Escape(b.Title), CsvUtil.Escape(b.Author), CsvUtil.Escape(b.Category),
                b.PublicationYear.ToString(), CsvUtil.Escape(b.ShelfLocation), b.TotalCopies.ToString(),
                b.AvailableCopies.ToString(), b.DamagedCopies.ToString(), b.Status, CsvUtil.Escape(b.WithdrawnReason)
            })).Append('\n');
        }
        return sb.ToString();
    }
}
