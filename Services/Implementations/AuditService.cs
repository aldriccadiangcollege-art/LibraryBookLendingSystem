using LibraryBookLendingSystem.Models.Entities;
using LibraryBookLendingSystem.Repositories.Interfaces;
using LibraryBookLendingSystem.Services.Interfaces;

namespace LibraryBookLendingSystem.Services.Implementations;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _repo;
    private readonly IHttpContextAccessor _http;

    public AuditService(IAuditRepository repo, IHttpContextAccessor http)
    {
        _repo = repo;
        _http = http;
    }

    public void Record(string action, string details, string? username = null)
    {
        var who = username ?? _http.HttpContext?.User.Identity?.Name ?? "anonymous";
        _repo.Add(new AuditLog
        {
            Timestamp = DateTime.Now,
            Username = who.Length > 50 ? who[..50] : who,
            Action = action,
            Details = details.Length > 500 ? details[..500] : details
        });
    }

    public Task SaveAsync() => _repo.SaveAsync();
}
