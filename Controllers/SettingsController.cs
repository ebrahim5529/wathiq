using HR.Data;
using HR.Models;
using HR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

public class SettingsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogService _audit;

    public SettingsController(ApplicationDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    // GET: /Settings
    public async Task<IActionResult> Index()
    {
        var settings = await _db.SystemSettings.ToListAsync();
        return View(settings);
    }

    // POST: /Settings/Save
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Dictionary<string, string> settings)
    {
        if (settings != null)
        {
            foreach (var kvp in settings)
            {
                var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == kvp.Key);
                if (setting != null)
                {
                    setting.Value = kvp.Value ?? string.Empty;
                }
                else
                {
                    _db.SystemSettings.Add(new SystemSetting
                    {
                        Key = kvp.Key,
                        Value = kvp.Value ?? string.Empty,
                        Group = "Custom"
                    });
                }
            }

            await _db.SaveChangesAsync();
            await _audit.LogAsync("تحديث إعدادات النظام", "Setting", null, "الإعدادات العامة", "تم تعديل خيارات وإعدادات النظام");
            TempData["Success"] = "تم حفظ إعدادات النظام بنجاح.";
        }

        return RedirectToAction(nameof(Index));
    }
}
