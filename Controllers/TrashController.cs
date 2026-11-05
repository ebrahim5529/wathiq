using HR.Data;
using HR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

public class TrashController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IStorageService _storage;
    private readonly IAuditLogService _audit;

    public TrashController(ApplicationDbContext db, IStorageService storage, IAuditLogService audit)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var deletedFolders = await _db.ArchiveFolders
            .Include(f => f.Department)
            .Where(f => f.IsDeleted)
            .OrderByDescending(f => f.DeletedAt)
            .ToListAsync();

        var deletedFiles = await _db.ArchiveFiles
            .Include(f => f.Category)
            .Include(f => f.Department)
            .Include(f => f.Folder)
            .Where(f => f.IsDeleted)
            .OrderByDescending(f => f.DeletedAt)
            .ToListAsync();

        ViewBag.DeletedFolders = deletedFolders;
        return View(deletedFiles);
    }

    // POST: /Trash/Restore
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, string itemType)
    {
        if (string.Equals(itemType, "Folder", StringComparison.OrdinalIgnoreCase))
        {
            var folder = await _db.ArchiveFolders.Include(f => f.Files).FirstOrDefaultAsync(f => f.Id == id);
            if (folder != null)
            {
                folder.IsDeleted = false;
                folder.DeletedAt = null;
                foreach (var f in folder.Files)
                {
                    f.IsDeleted = false;
                    f.DeletedAt = null;
                }
                await _db.SaveChangesAsync();
                await _audit.LogAsync("استعادة من السلة", "Folder", folder.Id, folder.Name, "تمت استعادة المجلد ومحتوياته من سلة المحذوفات");
                TempData["Success"] = $"تمت استعادة المجلد '{folder.Name}' بنجاح.";
            }
        }
        else
        {
            var file = await _db.ArchiveFiles.FindAsync(id);
            if (file != null)
            {
                file.IsDeleted = false;
                file.DeletedAt = null;
                await _db.SaveChangesAsync();
                await _audit.LogAsync("استعادة من السلة", "File", file.Id, file.Name, "تمت استعادة الملف من سلة المحذوفات");
                TempData["Success"] = $"تمت استعادة الملف '{file.Name}' بنجاح.";
            }
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Trash/DeletePermanently
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePermanently(int id, string itemType)
    {
        if (string.Equals(itemType, "Folder", StringComparison.OrdinalIgnoreCase))
        {
            var folder = await _db.ArchiveFolders.Include(f => f.Files).FirstOrDefaultAsync(f => f.Id == id);
            if (folder != null)
            {
                foreach (var file in folder.Files)
                {
                    _storage.DeletePhysicalFile(file.StoredName);
                    _db.ArchiveFiles.Remove(file);
                }
                _db.ArchiveFolders.Remove(folder);
                await _db.SaveChangesAsync();
                await _audit.LogAsync("حذف نهائي", "Folder", id, folder.Name, "تم حذف المجلد وجميع ملفاته نهائياً من النظام");
                TempData["Success"] = $"تم حذف المجلد '{folder.Name}' نهائياً.";
            }
        }
        else
        {
            var file = await _db.ArchiveFiles.FindAsync(id);
            if (file != null)
            {
                _storage.DeletePhysicalFile(file.StoredName);
                _db.ArchiveFiles.Remove(file);
                await _db.SaveChangesAsync();
                await _audit.LogAsync("حذف نهائي", "File", id, file.Name, "تم حذف الملف نهائياً من الخادم وقاعدة البيانات");
                TempData["Success"] = $"تم حذف الملف '{file.Name}' نهائياً.";
            }
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Trash/EmptyTrash
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmptyTrash()
    {
        var deletedFiles = await _db.ArchiveFiles.Where(f => f.IsDeleted).ToListAsync();
        foreach (var file in deletedFiles)
        {
            _storage.DeletePhysicalFile(file.StoredName);
            _db.ArchiveFiles.Remove(file);
        }

        var deletedFolders = await _db.ArchiveFolders.Where(f => f.IsDeleted).ToListAsync();
        _db.ArchiveFolders.RemoveRange(deletedFolders);

        await _db.SaveChangesAsync();
        await _audit.LogAsync("تفريغ السلة", "System", null, "سلة المحذوفات", $"تم تفريغ السلة وحذف {deletedFiles.Count} ملف و {deletedFolders.Count} مجلد نهائياً");

        TempData["Success"] = "تم تفريغ سلة المحذوفات بنجاح.";
        return RedirectToAction(nameof(Index));
    }
}
