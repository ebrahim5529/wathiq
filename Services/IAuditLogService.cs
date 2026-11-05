using HR.Data;
using HR.Models;
using Microsoft.EntityFrameworkCore;

namespace HR.Services;

public interface IAuditLogService
{
    Task LogAsync(string action, string itemType, int? itemId, string itemName, string details, string? userName = null, string? ipAddress = null);
    Task<List<ActivityLog>> GetRecentLogsAsync(int count = 20);
}

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string itemType, int? itemId, string itemName, string details, string? userName = null, string? ipAddress = null)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            var ip = ipAddress ?? context?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var user = userName ?? context?.User?.Identity?.Name ?? "مدير النظام";

            var log = new ActivityLog
            {
                UserName = user,
                Action = action,
                ItemType = itemType,
                ItemId = itemId,
                ItemName = itemName,
                Details = details,
                IpAddress = ip,
                CreatedAt = DateTime.UtcNow
            };

            _db.ActivityLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Logging should not crash application flow
        }
    }

    public async Task<List<ActivityLog>> GetRecentLogsAsync(int count = 20)
    {
        return await _db.ActivityLogs
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}

