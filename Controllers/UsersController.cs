using HR.Data;
using HR.Models;
using HR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

public class UsersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogService _audit;
    private readonly IPasswordHasher _passwordHasher;

    public UsersController(ApplicationDbContext db, IAuditLogService audit, IPasswordHasher passwordHasher)
    {
        _db = db;
        _audit = audit;
        _passwordHasher = passwordHasher;
    }

    // GET: /Users
    public async Task<IActionResult> Index()
    {
        var users = await _db.AppUsers
            .Include(u => u.Department)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        ViewBag.Departments = await _db.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
        ViewBag.Roles = await _db.AppRoles.OrderBy(r => r.Name).ToListAsync();

        return View(users);
    }

    // POST: /Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AppUser user, string? password)
    {
        if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.FullName) || string.IsNullOrWhiteSpace(user.Email))
        {
            TempData["Error"] = "جميع الحقول الأساسية مطلوبة.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            TempData["Error"] = "كلمة المرور مطلوبة ويجب ألا تقل عن 6 أحرف.";
            return RedirectToAction(nameof(Index));
        }

        user.Username = user.Username.Trim().ToLowerInvariant();
        user.Email = user.Email.Trim().ToLowerInvariant();

        if (await _db.AppUsers.AnyAsync(u => u.Username == user.Username))
        {
            TempData["Error"] = "اسم المستخدم مسجل مسبقاً.";
            return RedirectToAction(nameof(Index));
        }

        if (await _db.AppUsers.AnyAsync(u => u.Email == user.Email))
        {
            TempData["Error"] = "البريد الإلكتروني مسجل مسبقاً.";
            return RedirectToAction(nameof(Index));
        }

        user.PasswordHash = _passwordHasher.HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("إنشاء مستخدم", "User", user.Id, user.FullName, $"تم إنشاء حساب المستخدم {user.Username} بدور {user.Role}");
        TempData["Success"] = $"تم إنشاء حساب المستخدم '{user.FullName}' بنجاح مع تعيين كلمة المرور.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Users/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AppUser user, string? newPassword)
    {
        var existing = await _db.AppUsers.FindAsync(user.Id);
        if (existing == null)
        {
            TempData["Error"] = "المستخدم غير موجود.";
            return RedirectToAction(nameof(Index));
        }

        existing.FullName = user.FullName.Trim();
        existing.Email = user.Email.Trim().ToLowerInvariant();
        existing.Role = user.Role;
        existing.DepartmentId = user.DepartmentId;
        existing.IsActive = user.IsActive;

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            if (newPassword.Length < 6)
            {
                TempData["Error"] = "كلمة المرور الجديدة يجب ألا تقل عن 6 أحرف.";
                return RedirectToAction(nameof(Index));
            }
            existing.PasswordHash = _passwordHasher.HashPassword(newPassword);
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync("تعديل مستخدم", "User", existing.Id, existing.FullName, $"تم تحديث بيانات المستخدم {existing.Username}");
        TempData["Success"] = $"تم تحديث بيانات المستخدم '{existing.FullName}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Users/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            TempData["Error"] = "كلمة المرور يجب ألا تقل عن 6 أحرف.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _db.AppUsers.FindAsync(id);
        if (user == null)
        {
            TempData["Error"] = "المستخدم غير موجود.";
            return RedirectToAction(nameof(Index));
        }

        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("إعادة تعيين كلمة مرور", "User", user.Id, user.FullName, $"تمت إعادة تعيين كلمة مرور المستخدم {user.Username}");
        TempData["Success"] = $"تمت إعادة تعيين كلمة مرور المستخدم '{user.FullName}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Users/ToggleStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var user = await _db.AppUsers.FindAsync(id);
        if (user != null)
        {
            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = user.IsActive ? "تم تفعيل الحساب." : "تم تعطيل الحساب.";
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /Users/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.AppUsers.FindAsync(id);
        if (user != null)
        {
            _db.AppUsers.Remove(user);
            await _db.SaveChangesAsync();
            await _audit.LogAsync("حذف مستخدم", "User", id, user.FullName, $"تم حذف المستخدم {user.Username} نهائياً");
            TempData["Success"] = $"تم حذف المستخدم '{user.FullName}' بنجاح.";
        }
        return RedirectToAction(nameof(Index));
    }
}
