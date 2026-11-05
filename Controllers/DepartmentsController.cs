using HR.Data;
using HR.Models;
using HR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

public class DepartmentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogService _audit;

    public DepartmentsController(ApplicationDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    // GET: /Departments
    public async Task<IActionResult> Index()
    {
        var departments = await _db.Departments
            .Include(d => d.Folders)
            .Include(d => d.Files)
            .OrderBy(d => d.Name)
            .ToListAsync();

        return View(departments);
    }

    // POST: /Departments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Department department)
    {
        if (string.IsNullOrWhiteSpace(department.Name))
        {
            TempData["Error"] = "اسم القسم مطلوب.";
            return RedirectToAction(nameof(Index));
        }

        department.Name = department.Name.Trim();
        var exists = await _db.Departments.AnyAsync(d => d.Name == department.Name);
        if (exists)
        {
            TempData["Error"] = "يوجد قسم مسجل بهذا الاسم بالفعل.";
            return RedirectToAction(nameof(Index));
        }

        _db.Departments.Add(department);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("إنشاء قسم", "Department", department.Id, department.Name, "تمت إضافة قسم جديد للنظام");
        TempData["Success"] = $"تم إنشاء القسم '{department.Name}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Departments/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Department department)
    {
        if (string.IsNullOrWhiteSpace(department.Name))
        {
            TempData["Error"] = "اسم القسم مطلوب.";
            return RedirectToAction(nameof(Index));
        }

        var existing = await _db.Departments.FindAsync(department.Id);
        if (existing == null)
        {
            TempData["Error"] = "القسم غير موجود.";
            return RedirectToAction(nameof(Index));
        }

        existing.Name = department.Name.Trim();
        existing.Code = department.Code?.Trim();
        existing.IsActive = department.IsActive;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("تعديل قسم", "Department", existing.Id, existing.Name, "تم تحديث بيانات القسم");
        TempData["Success"] = $"تم تحديث بيانات القسم '{existing.Name}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Departments/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await _db.Departments
            .Include(d => d.Folders)
            .Include(d => d.Files)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
        {
            TempData["Error"] = "القسم غير موجود.";
            return RedirectToAction(nameof(Index));
        }

        var activeFiles = department.Files.Count(f => !f.IsDeleted);
        var activeFolders = department.Folders.Count(f => !f.IsDeleted);

        if (activeFiles > 0 || activeFolders > 0)
        {
            TempData["Error"] = $"لا يمكن حذف القسم '{department.Name}' لاحتوائه على {activeFolders} مجلد و {activeFiles} ملف. يمكنك تعطيله.";
            return RedirectToAction(nameof(Index));
        }

        _db.Departments.Remove(department);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("حذف قسم", "Department", id, department.Name, "تم حذف القسم نهائياً");
        TempData["Success"] = $"تم حذف القسم '{department.Name}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Departments/ToggleActive/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var dept = await _db.Departments.FindAsync(id);
        if (dept != null)
        {
            dept.IsActive = !dept.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = dept.IsActive ? "تم تفعيل القسم." : "تم تعطيل القسم.";
        }
        return RedirectToAction(nameof(Index));
    }
}
