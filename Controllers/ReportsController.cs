using System.Text;
using HR.Data;
using HR.Models;
using HR.Services;
using HR.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IStorageService _storage;

    public ReportsController(ApplicationDbContext db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    // GET: /Reports or /Reports/Statistics
    public async Task<IActionResult> Index()
    {
        return await Statistics();
    }

    public async Task<IActionResult> Statistics()
    {
        var totalFiles = await _db.ArchiveFiles.CountAsync(f => !f.IsDeleted);
        var totalFolders = await _db.ArchiveFolders.CountAsync(f => !f.IsDeleted);

        var byType = await _db.ArchiveFiles
            .Where(f => !f.IsDeleted)
            .GroupBy(f => f.DocumentType)
            .Select(g => new { Type = g.Key, Count = g.Count(), TotalSize = g.Sum(x => x.SizeBytes) })
            .ToListAsync();

        var byDept = await _db.ArchiveFiles
            .Where(f => !f.IsDeleted && f.Department != null)
            .GroupBy(f => f.Department!.Name)
            .Select(g => new { Dept = g.Key, Count = g.Count(), TotalSize = g.Sum(x => x.SizeBytes) })
            .ToListAsync();

        var byCat = await _db.ArchiveFiles
            .Where(f => !f.IsDeleted && f.Category != null)
            .GroupBy(f => f.Category!.Name)
            .Select(g => new { Cat = g.Key, Count = g.Count(), TotalSize = g.Sum(x => x.SizeBytes) })
            .ToListAsync();

        // Files uploaded in last 6 months
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
        var monthlyData = await _db.ArchiveFiles
            .Where(f => !f.IsDeleted && f.CreatedAt >= new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1))
            .GroupBy(f => new { f.CreatedAt.Year, f.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync();

        ViewBag.TotalFiles = totalFiles;
        ViewBag.TotalFolders = totalFolders;
        ViewBag.ByType = byType;
        ViewBag.ByDept = byDept;
        ViewBag.ByCat = byCat;
        ViewBag.MonthlyData = monthlyData;

        return View("Statistics");
    }

    // GET: /Reports/AuditLogs
    public async Task<IActionResult> AuditLogs(string? user, string? action, DateTime? from, DateTime? to)
    {
        var query = _db.ActivityLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(user))
        {
            query = query.Where(l => l.UserName.Contains(user));
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(l => l.Action.Contains(action));
        }

        if (from.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            var endOfDay = to.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(l => l.CreatedAt <= endOfDay);
        }

        var logs = await query.OrderByDescending(l => l.CreatedAt).Take(200).ToListAsync();

        ViewBag.SelectedUser = user;
        ViewBag.SelectedAction = action;
        ViewBag.SelectedFrom = from?.ToString("yyyy-MM-dd");
        ViewBag.SelectedTo = to?.ToString("yyyy-MM-dd");

        return View("AuditLogs", logs);
    }

    // GET: /Reports/Storage
    public async Task<IActionResult> Storage()
    {
        var totalDbBytes = await _db.ArchiveFiles.Where(f => !f.IsDeleted).SumAsync(f => f.SizeBytes);
        var (usedBytes, quotaBytes, percentage) = await _storage.GetStorageUsageAsync(totalDbBytes);

        var deptStats = await _db.Departments
            .Include(d => d.Files)
            .Select(d => new DepartmentStorageStat
            {
                DepartmentName = d.Name,
                FilesCount = d.Files.Count(f => !f.IsDeleted),
                SizeBytes = d.Files.Where(f => !f.IsDeleted).Sum(f => f.SizeBytes),
                PercentageOfTotal = totalDbBytes > 0 ? Math.Round(((double)d.Files.Where(f => !f.IsDeleted).Sum(f => f.SizeBytes) / totalDbBytes) * 100, 1) : 0
            })
            .OrderByDescending(s => s.SizeBytes)
            .ToListAsync();

        var catStats = await _db.Categories
            .Include(c => c.Files)
            .Select(c => new CategoryStorageStat
            {
                CategoryName = c.Name,
                Color = c.Color,
                FilesCount = c.Files.Count(f => !f.IsDeleted),
                SizeBytes = c.Files.Where(f => !f.IsDeleted).Sum(f => f.SizeBytes)
            })
            .OrderByDescending(s => s.SizeBytes)
            .ToListAsync();

        var extStats = await _db.ArchiveFiles
            .Where(f => !f.IsDeleted && f.Extension != null)
            .GroupBy(f => f.Extension!)
            .Select(g => new FileTypeStat
            {
                Extension = g.Key,
                Count = g.Count(),
                SizeBytes = g.Sum(f => f.SizeBytes)
            })
            .OrderByDescending(s => s.SizeBytes)
            .ToListAsync();

        var model = new StorageReportViewModel
        {
            TotalUsedBytes = usedBytes,
            QuotaBytes = quotaBytes,
            Percentage = percentage,
            DepartmentStats = deptStats,
            CategoryStats = catStats,
            FileTypeStats = extStats
        };

        return View("Storage", model);
    }

    // GET: /Reports/ExportCsv
    public async Task<IActionResult> ExportCsv(string type = "files")
    {
        var sb = new StringBuilder();

        if (type == "audit")
        {
            var logs = await _db.ActivityLogs.OrderByDescending(l => l.CreatedAt).Take(500).ToListAsync();
            sb.AppendLine("ID,المستخدم,الإجراء,نوع العنصر,اسم العنصر,التفاصيل,عنوان IP,التاريخ والوقت");
            foreach (var log in logs)
            {
                sb.AppendLine($"{log.Id},\"{log.UserName}\",\"{log.Action}\",\"{log.ItemType}\",\"{log.ItemName}\",\"{log.Details}\",\"{log.IpAddress}\",\"{log.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }
            return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(), "text/csv", $"AuditLogs_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
        else
        {
            var files = await _db.ArchiveFiles
                .Include(f => f.Category)
                .Include(f => f.Department)
                .Where(f => !f.IsDeleted)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            sb.AppendLine("ID,اسم الملف,الامتداد,الحجم (بايت),نوع المستند,الرقم المرجعي,التصنيف,القسم,الطرف الثاني,المرفوع بواسطة,تاريخ الرفع");
            foreach (var f in files)
            {
                sb.AppendLine($"{f.Id},\"{f.Name}\",\"{f.Extension}\",{f.SizeBytes},\"{f.DocumentType}\",\"{f.ReferenceNo}\",\"{f.Category?.Name}\",\"{f.Department?.Name}\",\"{f.Party}\",\"{f.CreatedBy}\",\"{f.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }
            return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(), "text/csv", $"ArchiveFiles_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
    }
}
