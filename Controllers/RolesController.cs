using HR.Data;
using HR.Models;
using HR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

public class RolesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogService _audit;

    public RolesController(ApplicationDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    // GET: /Roles
    public async Task<IActionResult> Index()
    {
        var roles = await _db.AppRoles.ToListAsync();
        var userCounts = await _db.AppUsers
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Role, x => x.Count);

        ViewBag.UserCounts = userCounts;
        return View(roles);
    }

    // POST: /Roles/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AppRole role)
    {
        var existing = await _db.AppRoles.FindAsync(role.Id);
        if (existing == null)
        {
            TempData["Error"] = "الدور غير موجود.";
            return RedirectToAction(nameof(Index));
        }

        existing.Name = role.Name.Trim();
        existing.Description = role.Description;
        existing.Permissions = role.Permissions;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("تعديل صلاحيات دور", "Role", existing.Id, existing.Name, $"تم تحديث صلاحيات الدور {existing.Code}");
        TempData["Success"] = $"تم تحديث صلاحيات الدور '{existing.Name}' بنجاح.";

        return RedirectToAction(nameof(Index));
    }
}
