using System.Text.RegularExpressions;
using HR.Data;
using HR.Helpers;
using HR.Models;
using HR.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

/// <summary>
/// مدير الملفات — قراءة شجرة المجلدات والمحتوى + العمليات الأساسية:
/// إنشاء مجلد، رفع ملفات، تنزيل، إعادة تسمية، حذف ناعم (إلى السلة).
/// </summary>
public class ArchiveController : Controller
{
    private static readonly Regex SafeExtension = new(@"^\.[a-z0-9]{1,10}$", RegexOptions.IgnorePatternWhitespace);

    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public ArchiveController(ApplicationDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    // ==================== صفحة مدير الملفات ====================

    public async Task<IActionResult> Index(int? folderId, string? type, string? search)
    {
        DocumentType? docType = !string.IsNullOrWhiteSpace(type)
            && Enum.TryParse<DocumentType>(type, ignoreCase: true, out var parsed)
            ? parsed
            : null;

        ArchiveFolder? current = null;
        if (folderId is int fid)
        {
            current = await _db.ArchiveFolders
                .FirstOrDefaultAsync(f => f.Id == fid && !f.IsDeleted);
            if (current == null) return NotFound();
        }

        // الشجرة وعدّادات الملفات
        var allFolders = await _db.ArchiveFolders.AsNoTracking()
            .Where(f => !f.IsDeleted)
            .OrderBy(f => f.Name)
            .ToListAsync();

        var fileCounts = await _db.ArchiveFiles.AsNoTracking()
            .Where(f => !f.IsDeleted && f.FolderId != null)
            .GroupBy(f => f.FolderId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        // محتوى المجلد الحالي
        var foldersQuery = _db.ArchiveFolders.AsNoTracking()
            .Where(f => !f.IsDeleted && f.ParentId == folderId);

        var filesQuery = _db.ArchiveFiles.AsNoTracking()
            .Include(f => f.Category)
            .Include(f => f.Department)
            .Where(f => !f.IsDeleted && f.FolderId == folderId);

        if (docType != null)
            filesQuery = filesQuery.Where(f => f.DocumentType == docType);

        search = search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            foldersQuery = foldersQuery.Where(f => f.Name.Contains(search));
            filesQuery = filesQuery.Where(f => f.Name.Contains(search));
        }

        var folders = await foldersQuery.OrderBy(f => f.Name).ToListAsync();
        var files = await filesQuery.OrderBy(f => f.CreatedAt).ToListAsync();

        // مسار التنقل من الجذر إلى المجلد الحالي
        var breadcrumb = new List<ArchiveFolder>();
        var walk = current;
        while (walk != null)
        {
            breadcrumb.Insert(0, walk);
            var pid = walk.ParentId;
            walk = pid == null ? null : allFolders.FirstOrDefault(x => x.Id == pid);
        }

        var usedBytes = await _db.ArchiveFiles.AsNoTracking()
            .Where(f => !f.IsDeleted)
            .SumAsync(f => (long?)f.SizeBytes) ?? 0L;

        var allowed = AllowedExtensions();

        var vm = new ArchiveIndexViewModel
        {
            CurrentFolder = current,
            AllFolders = allFolders,
            FileCounts = fileCounts,
            Folders = folders,
            Files = files,
            Breadcrumb = breadcrumb,
            DocumentType = docType,
            Search = search,
            UsedBytes = usedBytes,
            QuotaBytes = QuotaBytes,
            MaxFileBytes = MaxFileBytes,
            AllowedExtensions = allowed.ToList(),
            TotalItems = folders.Count + files.Count,
        };
        return View(vm);
    }

    // ==================== إنشاء مجلد ====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFolder(int? folderId, string? name)
    {
        name = name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            TempData["Error"] = "يرجى إدخال اسم المجلد.";
            return RedirectToAction(nameof(Index), new { folderId });
        }
        if (name.Length > 255)
        {
            TempData["Error"] = "اسم المجلد طويل جداً (الحد الأقصى 255 حرفاً).";
            return RedirectToAction(nameof(Index), new { folderId });
        }
        if (folderId != null && !await FolderExistsAsync(folderId))
        {
            TempData["Error"] = "المجلد الأب غير موجود.";
            return RedirectToAction(nameof(Index));
        }

        var duplicate = await _db.ArchiveFolders
            .AnyAsync(f => f.ParentId == folderId && !f.IsDeleted && f.Name == name);
        if (duplicate)
        {
            TempData["Error"] = $"يوجد مجلد بنفس الاسم \"{name}\" في هذا الموقع.";
            return RedirectToAction(nameof(Index), new { folderId });
        }

        _db.ArchiveFolders.Add(new ArchiveFolder
        {
            Name = name,
            ParentId = folderId,
            CreatedBy = CurrentUser,
            CreatedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"تم إنشاء المجلد \"{name}\" بنجاح.";
        return RedirectToAction(nameof(Index), new { folderId });
    }

    // ==================== رفع ملفات ====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(int? folderId, List<IFormFile>? files)
    {
        if (files is not { Count: > 0 })
        {
            TempData["Error"] = "لم يتم اختيار أي ملف للرفع.";
            return RedirectToAction(nameof(Index), new { folderId });
        }
        if (folderId != null && !await FolderExistsAsync(folderId))
        {
            TempData["Error"] = "المجلد غير موجود.";
            return RedirectToAction(nameof(Index));
        }

        var errors = new List<string>();
        var uploaded = 0;
        var allowed = AllowedExtensions();

        foreach (var file in files)
        {
            if (file is null || file.Length == 0) continue;

            // الاسم الأصلي (يُعرض للمستخدم) مع تقييد الطول
            var originalName = Path.GetFileName(file.FileName).Trim();
            if (originalName.Length > 255) originalName = originalName[..255];

            // الحجم
            if (file.Length > MaxFileBytes)
            {
                errors.Add($"\"{originalName}\": يتجاوز الحجم الأقصى ({FileUiHelper.FormatSize(MaxFileBytes)}).");
                continue;
            }

            // الامتداد (فحص منع Path Traversal عبر تعبير نمطي صارم)
            var ext = Path.GetExtension(originalName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !SafeExtension.IsMatch(ext) || !allowed.Contains(ext))
            {
                errors.Add($"\"{originalName}\": نوع الملف غير مسموح.");
                continue;
            }

            try
            {
                var now = DateTime.UtcNow;
                var storedName = $"{Guid.NewGuid():N}{ext}";
                var relativePath = $"{now.Year:0000}/{now.Month:00}/{storedName}";
                var dir = Path.Combine(UploadsRoot, now.Year.ToString(), now.Month.ToString("00"));
                Directory.CreateDirectory(dir);

                await using (var fs = System.IO.File.Create(Path.Combine(dir, storedName)))
                {
                    await file.CopyToAsync(fs);
                }

                _db.ArchiveFiles.Add(new ArchiveFile
                {
                    FolderId = folderId,
                    Name = originalName,
                    StoredName = relativePath,
                    Extension = ext.TrimStart('.'),
                    ContentType = file.ContentType,
                    SizeBytes = file.Length,
                    DocumentType = DocumentType.General,
                    CreatedBy = CurrentUser,
                    CreatedAt = now,
                });
                uploaded++;
            }
            catch (Exception)
            {
                errors.Add($"\"{originalName}\": تعذّر حفظ الملف.");
            }
        }

        if (uploaded > 0)
        {
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception)
            {
                TempData["Error"] = "تم حفظ الملفات على القرص لكن تعذّر تسجيلها في قاعدة البيانات.";
                return RedirectToAction(nameof(Index), new { folderId });
            }
        }

        if (errors.Count > 0) TempData["Error"] = string.Join(" ", errors);
        if (uploaded > 0) TempData["Success"] = $"تم رفع {uploaded} ملف بنجاح.";
        if (uploaded == 0 && errors.Count == 0) TempData["Error"] = "لم يتم رفع أي ملف.";

        return RedirectToAction(nameof(Index), new { folderId });
    }

    // ==================== تنزيل ملف ====================

    public async Task<IActionResult> Download(int id)
    {
        var file = await _db.ArchiveFiles.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
        if (file == null) return NotFound();

        // StoredName يُخزَّن كمسار نسبي تتحكم به الخادم، فلا يوجد Path Traversal
        var path = Path.Combine(UploadsRoot, file.StoredName.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(path)) return NotFound();

        var stream = System.IO.File.OpenRead(path);
        return File(stream, file.ContentType ?? "application/octet-stream", file.Name);
    }

    // ==================== إعادة تسمية ====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(string itemType, int id, string? newName, int? folderId)
    {
        newName = newName?.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            TempData["Error"] = "يرجى إدخال اسم جديد.";
            return RedirectToAction(nameof(Index), new { folderId });
        }
        if (newName.Length > 255)
        {
            TempData["Error"] = "الاسم طويل جداً (الحد الأقصى 255 حرفاً).";
            return RedirectToAction(nameof(Index), new { folderId });
        }

        if (itemType == "folder")
        {
            var folder = await _db.ArchiveFolders.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
            if (folder == null) return NotFound();

            var duplicate = await _db.ArchiveFolders
                .AnyAsync(f => f.ParentId == folder.ParentId && f.Id != folder.Id && !f.IsDeleted && f.Name == newName);
            if (duplicate)
            {
                TempData["Error"] = $"يوجد مجلد بنفس الاسم \"{newName}\" في نفس المستوى.";
                var backFolderId = folder.ParentId;
                return RedirectToAction(nameof(Index), new { folderId = backFolderId });
            }

            folder.Name = newName;
            folder.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"تمت إعادة تسمية المجلد إلى \"{newName}\".";
            return RedirectToAction(nameof(Index), new { folderId });
        }

        if (itemType == "file")
        {
            var file = await _db.ArchiveFiles.Include(f => f.Folder).FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
            if (file == null) return NotFound();
            file.Name = newName;
            file.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"تمت إعادة تسمية الملف إلى \"{newName}\".";
            return RedirectToAction(nameof(Index), new { folderId });
        }

        return NotFound();
    }

    // ==================== حذف ناعم (إلى السلة) ====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string itemType, int id, int? folderId)
    {
        if (itemType == "folder")
        {
            var folder = await _db.ArchiveFolders.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
            if (folder == null) return NotFound();

            // حذف ناعم متداخل لكل المجلدات الفرعية
            var allFolders = await _db.ArchiveFolders.Where(f => !f.IsDeleted).ToListAsync();
            var toDelete = new List<ArchiveFolder> { folder };
            var frontier = new List<int> { folder.Id };
            while (frontier.Count > 0)
            {
                var childIds = allFolders
                    .Where(f => f.ParentId != null && frontier.Contains(f.ParentId.Value))
                    .Select(f => f.Id)
                    .ToList();
                if (childIds.Count == 0) break;
                toDelete.AddRange(allFolders.Where(f => childIds.Contains(f.Id)));
                frontier = childIds;
            }

            var now = DateTime.UtcNow;
            foreach (var f in toDelete) { f.IsDeleted = true; f.DeletedAt = now; }
            await _db.SaveChangesAsync();

            TempData["Success"] = $"تم نقل المجلد \"{folder.Name}\" إلى سلة المحذوفات.";

            // إن كان المجلد المحذوف هو المجلد الحالي فارجع للأب
            if (folder.Id == folderId || BreadcrumbContains(allFolders, folder.Id, folderId))
                return RedirectToAction(nameof(Index), new { folderId = folder.ParentId });

            return RedirectToAction(nameof(Index), new { folderId });
        }

        if (itemType == "file")
        {
            var file = await _db.ArchiveFiles.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
            if (file == null) return NotFound();
            file.IsDeleted = true;
            file.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"تم نقل الملف \"{file.Name}\" إلى سلة المحذوفات.";
            return RedirectToAction(nameof(Index), new { folderId });
        }

        return NotFound();
    }

    // ==================== أدوات خاصة ====================

    private string CurrentUser =>
        User?.Identity is { IsAuthenticated: true, Name: { Length: > 0 } name } ? name : "النظام";

    private string UploadsRoot =>
        Path.Combine(_env.ContentRootPath, _config["Archive:UploadsRoot"] ?? "uploads");

    private long MaxFileBytes =>
        _config.GetValue<long?>("Archive:MaxFileSizeBytes") ?? 10L * 1024 * 1024;

    private long QuotaBytes =>
        _config.GetValue<long?>("Archive:StorageQuotaBytes") ?? 10L * 1024 * 1024 * 1024;

    private static readonly string[] DefaultExtensions =
    [
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".ppt", ".pptx",
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".zip", ".rar", ".7z", ".txt", ".md"
    ];

    /// <summary>الامتدادات المسموحة من الإعدادات (مصفوفة JSON تُقرأ عبر GetSection).</summary>
    private string[] AllowedExtensions()
    {
        var configured = _config.GetSection("Archive:AllowedExtensions").Get<string[]>();
        var list = configured is { Length: > 0 } ? configured : DefaultExtensions;
        return list
            .Select(e => e.Trim().ToLowerInvariant())
            .Where(e => e.StartsWith('.') && e.Length > 1)
            .Distinct()
            .ToArray();
    }

    private async Task<bool> FolderExistsAsync(int? folderId) =>
        folderId == null || await _db.ArchiveFolders.AnyAsync(f => f.Id == folderId && !f.IsDeleted);

    /// <summary>هل المجلد المعطّل الوسيط (ancestorId) هو سلف للمجلد الحالي؟</summary>
    private static bool BreadcrumbContains(List<ArchiveFolder> allFolders, int ancestorId, int? folderId)
    {
        var currentId = folderId;
        while (currentId != null)
        {
            if (currentId == ancestorId) return true;
            var parent = allFolders.FirstOrDefault(f => f.Id == currentId)?.ParentId;
            currentId = parent;
        }
        return false;
    }
}
