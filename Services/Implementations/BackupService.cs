using LibraryBookLendingSystem.Data;
using LibraryBookLendingSystem.Exceptions;
using LibraryBookLendingSystem.Models;
using LibraryBookLendingSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LibraryBookLendingSystem.Services.Implementations;

/// <summary>Creates a full SQL Server backup (.bak) of the library database in the configured folder.</summary>
public class BackupService : IBackupService
{
    private readonly LibraryDbContext _db;
    private readonly IAuditService _audit;
    private readonly LibrarySettings _rules;

    public BackupService(LibraryDbContext db, IAuditService audit, IOptions<LibrarySettings> rules)
    {
        _db = db;
        _audit = audit;
        _rules = rules.Value;
    }

    public async Task<string> BackupAsync()
    {
        var dbName = _db.Database.GetDbConnection().Database;
        var file = Path.Combine(_rules.BackupFolder, $"{dbName}_{DateTime.Now:yyyyMMdd_HHmmss}.bak");

        try
        {
            Directory.CreateDirectory(_rules.BackupFolder);
            // Folder and file name are built by the server (never from user input).
            var sql = "BACKUP DATABASE [" + dbName.Replace("]", "]]") + "] TO DISK = N'" + file.Replace("'", "''") + "' WITH INIT, CHECKSUM";
            await _db.Database.ExecuteSqlRawAsync(sql);
        }
        catch (Exception ex)
        {
            throw new ConflictException("Backup failed: " + ex.Message);
        }

        _audit.Record("Backup", $"Backup written to {file}.");
        await _audit.SaveAsync();
        return file;
    }
}
