using HR.Data;
using HR.Models;
using HR.Services;
using HR.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

public class ArchiveController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IStorageService _storage;
    private readonly IAuditLogService _audit;

    public ArchiveController(ApplicationDbContext db, IStorageService storage, IAuditLogService audit)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
    }

    // GET: /Archive
    public async Task<IActionResult> Index(int? folderId, DocumentType? type, string? section, string? q)
    {
        var model = new FileManagerViewModel
        {
            FilterDocumentType = type,
            ActiveSection = section ?? (type.HasValue ? type.Value.ToString().ToLower() : "all")
        };

        // Breadcrumbs
        if (folderId.HasValue)
        {
            var current = await _db.ArchiveFolders
                .Include(f => f.Department)
                .FirstOrDefaultAsync(f => f.Id == folderId.Value && !f.IsDeleted);

            if (current != null)
            {
                model.CurrentFolder = current;
                var crumbs = new List<ArchiveFolder> { current };
                var parentId = current.ParentId;
                while (parentId.HasValue)
                {
                    var parent = await _db.ArchiveFolders.FirstOrDefaultAsync(f => f.Id == parentId.Value && !f.IsDeleted);
                    if (parent == null) break;
                    crumbs.Insert(0, parent);
                    parentId = parent.ParentId;
                }
                model.Breadcrumbs = crumbs;
            }
        }

        // Subfolders
        var foldersQuery = _db.ArchiveFolders
            .Include(f => f.Department)
            .Include(f => f.Files)
            .Where(f => !f.IsDeleted);

        if (folderId.HasValue)
        {
            foldersQuery = foldersQuery.Where(f => f.ParentId == folderId.Value);
        }
        else
        {
            foldersQuery = foldersQuery.Where(f => f.ParentId == null);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            foldersQuery = foldersQuery.Where(f => f.Name.Contains(q));
        }

        model.Folders = await foldersQuery.OrderBy(f => f.Name).ToListAsync();

        // Files
        var filesQuery = _db.ArchiveFiles
            .Include(f => f.Category)
            .Include(f => f.Department)
            .Where(f => !f.IsDeleted);

        if (folderId.HasValue)
        {
            filesQuery = filesQuery.Where(f => f.FolderId == folderId.Value);
        }
        else if (!type.HasValue && string.IsNullOrEmpty(q))
        {
            // At root, show files that have no folder or show all root files
            filesQuery = filesQuery.Where(f => f.FolderId == null);
        }

        if (type.HasValue)
        {
            filesQuery = filesQuery.Where(f => f.DocumentType == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            filesQuery = filesQuery.Where(f => f.Name.Contains(q) || (f.ReferenceNo != null && f.ReferenceNo.Contains(q)) || (f.Party != null && f.Party.Contains(q)));
        }

        model.Files = await filesQuery.OrderByDescending(f => f.CreatedAt).ToListAsync();

        // Categories & Departments for modals
        model.Categories = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
        model.Departments = await _db.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();

        // Storage calculations
        var totalDbBytes = await _db.ArchiveFiles.Where(f => !f.IsDeleted).SumAsync(f => f.SizeBytes);
        var storageInfo = await _storage.GetStorageUsageAsync(totalDbBytes);
        model.StorageUsedBytes = storageInfo.usedBytes;
        model.StorageQuotaBytes = storageInfo.quotaBytes;
        model.StoragePercentage = storageInfo.percentage;

        // Custom title based on type filter
        if (type.HasValue)
        {
            model.Title = type.Value switch
            {
                DocumentType.Official => "المستندات الرسمية",
                DocumentType.Contract => "العقود والاتفاقيات",
                DocumentType.Correspondence => "المراسلات والخطابات",
                DocumentType.Invoice => "الفواتير والمستندات المالية",
                _ => "مدير الملفات"
            };
        }

        return View(model);
    }

    // POST: /Archive/CreateFolder
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFolder(string name, int? parentId, int? departmentId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "اسم المجلد مطلوب.";
            return RedirectToAction(nameof(Index), new { folderId = parentId });
        }

        var folderName = name.Trim();
        var exists = await _db.ArchiveFolders
            .AnyAsync(f => f.ParentId == parentId && f.Name == folderName && !f.IsDeleted);

        if (exists)
        {
            TempData["Error"] = "يوجد مجلد آخر بنفس الاسم في هذا المسار.";
            return RedirectToAction(nameof(Index), new { folderId = parentId });
        }

        var folder = new ArchiveFolder
        {
            Name = folderName,
            ParentId = parentId,
            DepartmentId = departmentId,
            CreatedBy = User.Identity?.Name ?? "مدير النظام",
            CreatedAt = DateTime.UtcNow
        };

        _db.ArchiveFolders.Add(folder);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("إنشاء مجلد", "Folder", folder.Id, folder.Name, $"تم إنشاء مجلد جديد في المسار {(parentId.HasValue ? parentId.Value.ToString() : "الرئيسي")}");

        TempData["Success"] = $"تم إنشاء المجلد '{folder.Name}' بنجاح.";
        return RedirectToAction(nameof(Index), new { folderId = parentId });
    }

    // POST: /Archive/UploadFiles
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadFiles(
        List<IFormFile> files,
        int? folderId,
        int? categoryId,
        int? departmentId,
        DocumentType documentType,
        string? referenceNo,
        string? description,
        string? party,
        DateTime? documentDate,
        DateTime? expiryDate,
        decimal? amount,
        string? currency,
        string? tags)
    {
        if (files == null || files.Count == 0)
        {
            TempData["Error"] = "يرجى تحديد ملف واحد على الأقل للرفع.";
            return RedirectToAction(nameof(Index), new { folderId });
        }

        int uploadedCount = 0;
        var userName = User.Identity?.Name ?? "مدير النظام";

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            try
            {
                var (storedName, sizeBytes, contentType, ext) = await _storage.SaveFileAsync(file);

                var archiveFile = new ArchiveFile
                {
                    Name = Path.GetFileName(file.FileName),
                    StoredName = storedName,
                    Extension = ext,
                    ContentType = contentType,
                    SizeBytes = sizeBytes,
                    FolderId = folderId,
                    CategoryId = categoryId,
                    DepartmentId = departmentId,
                    DocumentType = documentType,
                    ReferenceNo = referenceNo,
                    Description = description,
                    Party = party,
                    DocumentDate = documentDate,
                    ExpiryDate = expiryDate,
                    Amount = amount,
                    Currency = string.IsNullOrWhiteSpace(currency) ? "SAR" : currency,
                    Tags = tags,
                    CreatedBy = userName,
                    CreatedAt = DateTime.UtcNow
                };

                _db.ArchiveFiles.Add(archiveFile);
                await _db.SaveChangesAsync();

                await _audit.LogAsync("رفع ملف", "File", archiveFile.Id, archiveFile.Name, $"تم رفع ملف بحجم {Math.Round(sizeBytes / 1024.0, 1)} KB بنوع {documentType}");
                uploadedCount++;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطأ أثناء رفع '{file.FileName}': {ex.Message}";
            }
        }

        if (uploadedCount > 0)
        {
            TempData["Success"] = $"تم رفع {uploadedCount} ملف(ات) بنجاح.";
        }

        return RedirectToAction(nameof(Index), new { folderId });
    }

    // GET: /Archive/DownloadFile/5
    public async Task<IActionResult> DownloadFile(int id)
    {
        var file = await _db.ArchiveFiles.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
        if (file == null)
        {
            TempData["Error"] = "الملف غير موجود أو تم حذفه.";
            return RedirectToAction(nameof(Index));
        }

        var download = _storage.GetFileDownload(file);
        if (download == null)
        {
            TempData["Error"] = "تعذر العثور على الملف الفعلي في الخادم.";
            return RedirectToAction(nameof(Index), new { folderId = file.FolderId });
        }

        await _audit.LogAsync("تنزيل ملف", "File", file.Id, file.Name, $"تم تنزيل الملف بواسطة {User.Identity?.Name ?? "المستخدم"}");

        return File(download.Value.Stream, download.Value.ContentType, download.Value.DownloadName);
    }

    // GET: /Archive/DownloadFolder/5
    public async Task<IActionResult> DownloadFolder(int id)
    {
        var folder = await _db.ArchiveFolders
            .Include(f => f.Files)
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        if (folder == null)
        {
            TempData["Error"] = "المجلد غير موجود.";
            return RedirectToAction(nameof(Index));
        }

        var activeFiles = folder.Files.Where(f => !f.IsDeleted).ToList();
        if (activeFiles.Count == 0)
        {
            TempData["Error"] = "المجلد فارغ لا يحتوي على ملفات للتنزيل.";
            return RedirectToAction(nameof(Index), new { folderId = id });
        }

        var (zipStream, zipName) = await _storage.CreateZipFromFilesAsync(activeFiles, folder.Name);
        await _audit.LogAsync("تنزيل مجلد مضغوط", "Folder", folder.Id, folder.Name, $"تم تنزيل المجلد بحجم {activeFiles.Count} ملف(ات)");

        return File(zipStream, "application/zip", zipName);
    }

    // POST: /Archive/Rename
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(int id, string itemType, string newName, int? returnFolderId)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            TempData["Error"] = "الاسم الجديد غير صالح.";
            return RedirectToAction(nameof(Index), new { folderId = returnFolderId });
        }

        newName = newName.Trim();

        if (string.Equals(itemType, "Folder", StringComparison.OrdinalIgnoreCase))
        {
            var folder = await _db.ArchiveFolders.FindAsync(id);
            if (folder != null)
            {
                var oldName = folder.Name;
                folder.Name = newName;
                folder.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                await _audit.LogAsync("إعادة تسمية مجلد", "Folder", folder.Id, folder.Name, $"تم تغيير الاسم من '{oldName}' إلى '{newName}'");
                TempData["Success"] = "تمت إعادة تسمية المجلد بنجاح.";
            }
        }
        else
        {
            var file = await _db.ArchiveFiles.FindAsync(id);
            if (file != null)
            {
                var oldName = file.Name;
                file.Name = newName;
                file.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                await _audit.LogAsync("إعادة تسمية ملف", "File", file.Id, file.Name, $"تم تغيير الاسم من '{oldName}' إلى '{newName}'");
                TempData["Success"] = "تمت إعادة تسمية الملف بنجاح.";
            }
        }

        return RedirectToAction(nameof(Index), new { folderId = returnFolderId });
    }

    // POST: /Archive/ToggleFavorite
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFavorite(int id, string itemType, int? returnFolderId)
    {
        if (string.Equals(itemType, "Folder", StringComparison.OrdinalIgnoreCase))
        {
            var folder = await _db.ArchiveFolders.FindAsync(id);
            if (folder != null)
            {
                folder.IsFavorite = !folder.IsFavorite;
                await _db.SaveChangesAsync();
                TempData["Success"] = folder.IsFavorite ? "تمت إضافة المجلد إلى المفضلة." : "تمت إزالة المجلد من المفضلة.";
            }
        }
        else
        {
            var file = await _db.ArchiveFiles.FindAsync(id);
            if (file != null)
            {
                file.IsFavorite = !file.IsFavorite;
                await _db.SaveChangesAsync();
                TempData["Success"] = file.IsFavorite ? "تمت إضافة الملف إلى المفضلة." : "تمت إزالة الملف من المفضلة.";
            }
        }

        return RedirectToAction(nameof(Index), new { folderId = returnFolderId });
    }

    // POST: /Archive/Delete (Soft delete)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string itemType, int? returnFolderId)
    {
        if (string.Equals(itemType, "Folder", StringComparison.OrdinalIgnoreCase))
        {
            var folder = await _db.ArchiveFolders.Include(f => f.Files).FirstOrDefaultAsync(f => f.Id == id);
            if (folder != null)
            {
                folder.IsDeleted = true;
                folder.DeletedAt = DateTime.UtcNow;
                // Soft delete child files as well
                foreach (var f in folder.Files)
                {
                    f.IsDeleted = true;
                    f.DeletedAt = DateTime.UtcNow;
                }
                await _db.SaveChangesAsync();
                await _audit.LogAsync("حذف إلى السلة", "Folder", folder.Id, folder.Name, "تم نقل المجلد ومحتوياته إلى سلة المحذوفات");
                TempData["Success"] = $"تم نقل المجلد '{folder.Name}' إلى سلة المحذوفات.";
            }
        }
        else
        {
            var file = await _db.ArchiveFiles.FindAsync(id);
            if (file != null)
            {
                file.IsDeleted = true;
                file.DeletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                await _audit.LogAsync("حذف إلى السلة", "File", file.Id, file.Name, "تم نقل الملف إلى سلة المحذوفات");
                TempData["Success"] = $"تم نقل الملف '{file.Name}' إلى سلة المحذوفات.";
            }
        }

        return RedirectToAction(nameof(Index), new { folderId = returnFolderId });
    }

    // POST: /Archive/Share
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Share(int itemId, string itemType, string? sharedWithUser, int? departmentId, string permission, string? notes, int? returnFolderId)
    {
        var share = new ArchiveShare
        {
            ItemType = itemType,
            FileId = string.Equals(itemType, "File", StringComparison.OrdinalIgnoreCase) ? itemId : null,
            FolderId = string.Equals(itemType, "Folder", StringComparison.OrdinalIgnoreCase) ? itemId : null,
            SharedWithUser = sharedWithUser,
            SharedWithDepartmentId = departmentId,
            Permission = string.IsNullOrWhiteSpace(permission) ? "View" : permission,
            SharedBy = User.Identity?.Name ?? "مدير النظام",
            Notes = notes,
            SharedAt = DateTime.UtcNow
        };

        _db.ArchiveShares.Add(share);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("مشاركة عنصر", itemType, itemId, $"عنصر #{itemId}", $"تمت المشاركة بصلاحية {permission}");
        TempData["Success"] = "تمت مشاركة العنصر بنجاح.";

        return RedirectToAction(nameof(Index), new { folderId = returnFolderId });
    }
}
