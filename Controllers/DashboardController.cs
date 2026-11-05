using HR.Data;
using HR.Models;
using HR.Services;
using HR.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IStorageService _storage;

    public DashboardController(ApplicationDbContext db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var startOfToday = now.Date;
        var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);

        var totalFiles = await _db.ArchiveFiles.CountAsync(f => !f.IsDeleted);
        var totalFolders = await _db.ArchiveFolders.CountAsync(f => !f.IsDeleted);
        var totalCategories = await _db.Categories.CountAsync(c => c.IsActive);
        var totalDepartments = await _db.Departments.CountAsync(d => d.IsActive);

        var uploadedToday = await _db.ArchiveFiles.CountAsync(f => !f.IsDeleted && f.CreatedAt >= startOfToday);
        var uploadedThisWeek = await _db.ArchiveFiles.CountAsync(f => !f.IsDeleted && f.CreatedAt >= startOfWeek);

        var totalSizeBytes = await _db.ArchiveFiles.Where(f => !f.IsDeleted).SumAsync(f => f.SizeBytes);
        var (usedBytes, quotaBytes, percentage) = await _storage.GetStorageUsageAsync(totalSizeBytes);

        // Document Type distribution
        var docTypes = await _db.ArchiveFiles
            .Where(f => !f.IsDeleted)
            .GroupBy(f => f.DocumentType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        var typeDist = Enum.GetValues<DocumentType>().ToDictionary(t => t, t => 0);
        foreach (var item in docTypes)
        {
            typeDist[item.Type] = item.Count;
        }

        // Department distribution
        var deptDist = await _db.ArchiveFiles
            .Where(f => !f.IsDeleted && f.Department != null)
            .GroupBy(f => f.Department!.Name)
            .Select(g => new { Dept = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Dept, g => g.Count);

        // Recent files
        var recentFiles = await _db.ArchiveFiles
            .Include(f => f.Category)
            .Include(f => f.Department)
            .Where(f => !f.IsDeleted)
            .OrderByDescending(f => f.CreatedAt)
            .Take(8)
            .ToListAsync();

        // Expiring contracts (within 30 days)
        var thirtyDaysLater = now.AddDays(30);
        var expiringContracts = await _db.ArchiveFiles
            .Include(f => f.Department)
            .Where(f => !f.IsDeleted && f.DocumentType == DocumentType.Contract && f.ExpiryDate != null && f.ExpiryDate >= now && f.ExpiryDate <= thirtyDaysLater)
            .OrderBy(f => f.ExpiryDate)
            .Take(5)
            .ToListAsync();

        // Recent Activity Logs
        var recentLogs = await _db.ActivityLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(8)
            .ToListAsync();

        var model = new DashboardViewModel
        {
            TotalFiles = totalFiles,
            TotalFolders = totalFolders,
            TotalCategories = totalCategories,
            TotalDepartments = totalDepartments,
            FilesUploadedToday = uploadedToday,
            FilesUploadedThisWeek = uploadedThisWeek,
            StorageUsedBytes = usedBytes,
            StorageQuotaBytes = quotaBytes,
            StoragePercentage = percentage,
            DocumentTypeDistribution = typeDist,
            DepartmentDistribution = deptDist,
            RecentFiles = recentFiles,
            ExpiringContracts = expiringContracts,
            RecentActivityLogs = recentLogs
        };

        return View(model);
    }
}
