using HR.Data;
using HR.Models;
using HR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogService _audit;

    public CategoriesController(ApplicationDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    // GET: /Categories
    public async Task<IActionResult> Index()
    {
        var categories = await _db.Categories
            .Include(c => c.Files)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(categories);
    }

    // POST: /Categories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
        {
            TempData["Error"] = "اسم التصنيف مطلوب.";
            return RedirectToAction(nameof(Index));
        }

        category.Name = category.Name.Trim();
        var exists = await _db.Categories.AnyAsync(c => c.Name == category.Name);
        if (exists)
        {
            TempData["Error"] = "يوجد تصنيف آخر يحمل هذا الاسم بالفعل.";
            return RedirectToAction(nameof(Index));
        }

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("إنشاء تصنيف", "Category", category.Id, category.Name, "تمت إضافة تصنيف جديد للأرشيف");
        TempData["Success"] = $"تمت إضافة التصنيف '{category.Name}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Categories/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
        {
            TempData["Error"] = "اسم التصنيف مطلوب.";
            return RedirectToAction(nameof(Index));
        }

        var existing = await _db.Categories.FindAsync(category.Id);
        if (existing == null)
        {
            TempData["Error"] = "التصنيف غير موجود.";
            return RedirectToAction(nameof(Index));
        }

        existing.Name = category.Name.Trim();
        existing.Description = category.Description;
        existing.Color = category.Color;
        existing.Icon = category.Icon;
        existing.IsActive = category.IsActive;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("تعديل تصنيف", "Category", existing.Id, existing.Name, "تم تحديث بيانات التصنيف");
        TempData["Success"] = $"تم تحديث التصنيف '{existing.Name}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Categories/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Files).FirstOrDefaultAsync(c => c.Id == id);
        if (category == null)
        {
            TempData["Error"] = "التصنيف غير موجود.";
            return RedirectToAction(nameof(Index));
        }

        var activeFilesCount = category.Files.Count(f => !f.IsDeleted);
        if (activeFilesCount > 0)
        {
            TempData["Error"] = $"لا يمكن حذف التصنيف '{category.Name}' لوجود {activeFilesCount} ملف(ات) مرتبطة به. يمكنك تعطيله بدلاً من حذفه.";
            return RedirectToAction(nameof(Index));
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("حذف تصنيف", "Category", id, category.Name, "تم حذف التصنيف نهائياً");
        TempData["Success"] = $"تم حذف التصنيف '{category.Name}' بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Categories/ToggleActive/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category != null)
        {
            category.IsActive = !category.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = category.IsActive ? "تم تفعيل التصنيف." : "تم تعطيل التصنيف.";
        }
        return RedirectToAction(nameof(Index));
    }
}
