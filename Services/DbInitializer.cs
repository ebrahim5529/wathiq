using HR.Data;
using HR.Models;
using Microsoft.EntityFrameworkCore;

namespace HR.Services;

public interface IDbInitializer
{
    Task SeedAsync();
}

public class DbInitializer : IDbInitializer
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DbInitializer> _logger;
    private readonly IPasswordHasher _passwordHasher;

    public DbInitializer(ApplicationDbContext db, ILogger<DbInitializer> logger, IPasswordHasher passwordHasher)
    {
        _db = db;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Seed Settings if not present
            if (!await _db.SystemSettings.AnyAsync())
            {
                _db.SystemSettings.AddRange(
                    new SystemSetting { Key = "SystemName", Value = "نظام وثيق للأرشفة الإلكترونية", Group = "General", Description = "اسم المنظومة" },
                    new SystemSetting { Key = "MaxUploadSizeMB", Value = "50", Group = "Storage", Description = "الحد الأقصى لرفع الملف بالميجابايت" },
                    new SystemSetting { Key = "StorageQuotaGB", Value = "50", Group = "Storage", Description = "الحصة التخزينية الإجمالية بالجيجابايت" },
                    new SystemSetting { Key = "AllowedExtensions", Value = ".pdf,.doc,.docx,.xls,.xlsx,.png,.jpg,.jpeg,.zip,.rar", Group = "Storage", Description = "الامتدادات المسموح بها" },
                    new SystemSetting { Key = "TrashRetentionDays", Value = "30", Group = "Storage", Description = "أيام الاحتفاظ في السلة قبل الحذف النهائي" },
                    new SystemSetting { Key = "ContractAlertDays", Value = "15", Group = "Notifications", Description = "تنبيه قبل انتهاء العقد بالأيام" }
                );
            }

            // Seed Roles
            if (!await _db.AppRoles.AnyAsync())
            {
                _db.AppRoles.AddRange(
                    new AppRole { Code = "Admin", Name = "مدير النظام", Description = "كامل الصلاحيات على جميع الشاشات والإعدادات", Permissions = "All" },
                    new AppRole { Code = "Archivist", Name = "أمين الأرشيف", Description = "إدارة المحتوى والمستندات والرفع والتصنيف والاستعادة", Permissions = "ManageArchive,Upload,Download,Restore" },
                    new AppRole { Code = "DeptSupervisor", Name = "مشرف قسم", Description = "إدارة وعرض ملفات ومستندات قسمه فقط", Permissions = "DeptAccess,Upload,Download,Share" },
                    new AppRole { Code = "Employee", Name = "مستخدم يومي", Description = "عرض المسموح به ورفع ملفات جديدة ومفضلة", Permissions = "View,Upload,Favorite" },
                    new AppRole { Code = "Guest", Name = "زائر / قراءة فقط", Description = "اطلاع وتنزيل حسب الصلاحيات الممنوحة", Permissions = "View,Download" }
                );
            }

            // Seed Departments
            if (!await _db.Departments.AnyAsync())
            {
                var hr = new Department { Name = "الموارد البشرية", Code = "HR", IsActive = true };
                var legal = new Department { Name = "الشؤون القانونية", Code = "LEGAL", IsActive = true };
                var finance = new Department { Name = "الإدارة المالية والمحاسبة", Code = "FIN", IsActive = true };
                var executive = new Department { Name = "الإدارة التنفيذية", Code = "EXEC", IsActive = true };
                var it = new Department { Name = "تقنية المعلومات", Code = "IT", IsActive = true };

                _db.Departments.AddRange(hr, legal, finance, executive, it);
                await _db.SaveChangesAsync();

                // Seed Users
                if (!await _db.AppUsers.AnyAsync())
                {
                    _db.AppUsers.AddRange(
                        new AppUser { Username = "admin", FullName = "عبدالرحمن الماجد (مدير النظام)", Email = "admin@wathiq.local", PasswordHash = _passwordHasher.HashPassword("admin123"), Role = "Admin", DepartmentId = it.Id, IsActive = true },
                        new AppUser { Username = "archivist", FullName = "سارة السليمان (أمينة الأرشيف)", Email = "sarah@wathiq.local", PasswordHash = _passwordHasher.HashPassword("archivist123"), Role = "Archivist", DepartmentId = hr.Id, IsActive = true },
                        new AppUser { Username = "legal_head", FullName = "خالد العتيبي (مستشار قانوني)", Email = "khaled@wathiq.local", PasswordHash = _passwordHasher.HashPassword("legal123"), Role = "DeptSupervisor", DepartmentId = legal.Id, IsActive = true },
                        new AppUser { Username = "finance_user", FullName = "محمد الشمري (محاسب)", Email = "mohammed@wathiq.local", PasswordHash = _passwordHasher.HashPassword("finance123"), Role = "Employee", DepartmentId = finance.Id, IsActive = true }
                    );
                }
                else
                {
                    // Ensure existing users without password have passwords set
                    var usersWithoutPassword = await _db.AppUsers.Where(u => string.IsNullOrEmpty(u.PasswordHash)).ToListAsync();
                    foreach (var u in usersWithoutPassword)
                    {
                        var defaultPass = u.Username switch
                        {
                            "admin" => "admin123",
                            "archivist" => "archivist123",
                            "legal_head" => "legal123",
                            "finance_user" => "finance123",
                            _ => $"{u.Username}123"
                        };
                        u.PasswordHash = _passwordHasher.HashPassword(defaultPass);
                    }
                    if (usersWithoutPassword.Any())
                    {
                        await _db.SaveChangesAsync();
                    }
                }

                // Seed Categories
                if (!await _db.Categories.AnyAsync())
                {
                    _db.Categories.AddRange(
                        new Category { Name = "ملفات الموظفين", Color = "#0d6efd", Icon = "bi-person-badge", Description = "هويات وعقود ومستندات منسوبي المنشأة", IsActive = true },
                        new Category { Name = "عقود واتفاقيات", Color = "#198754", Icon = "bi-file-earmark-ruled", Description = "العقود والاتفاقيات الرسمية المبرمة", IsActive = true },
                        new Category { Name = "مراسلات وقرارات", Color = "#0dcaf0", Icon = "bi-envelope-paper", Description = "الخطابات والقرارات الإدارية والتعاميم", IsActive = true },
                        new Category { Name = "فواتير ومستخلصات", Color = "#ffc107", Icon = "bi-receipt", Description = "الفواتير الضريبية ومطالبات الموردين", IsActive = true },
                        new Category { Name = "تراخيص وشهادات", Color = "#6f42c1", Icon = "bi-award", Description = "السجلات التجارية والرخص الرسمية", IsActive = true }
                    );
                    await _db.SaveChangesAsync();
                }

                // Seed Sample Folders
                if (!await _db.ArchiveFolders.AnyAsync())
                {
                    var f1 = new ArchiveFolder { Name = "العقود الرسمية 2026", DepartmentId = legal.Id, CreatedBy = "admin" };
                    var f2 = new ArchiveFolder { Name = "فواتير الموردين والمشتريات", DepartmentId = finance.Id, CreatedBy = "admin" };
                    var f3 = new ArchiveFolder { Name = "قرارات مجلس الإدارة", DepartmentId = executive.Id, CreatedBy = "admin" };
                    var f4 = new ArchiveFolder { Name = "ملفات الموظفين والرواتب", DepartmentId = hr.Id, CreatedBy = "admin" };

                    _db.ArchiveFolders.AddRange(f1, f2, f3, f4);
                    await _db.SaveChangesAsync();

                    var catLegal = await _db.Categories.FirstOrDefaultAsync(c => c.Name == "عقود واتفاقيات");
                    var catFin = await _db.Categories.FirstOrDefaultAsync(c => c.Name == "فواتير ومستخلصات");
                    var catHR = await _db.Categories.FirstOrDefaultAsync(c => c.Name == "ملفات الموظفين");

                    // Seed Sample Archive Files
                    _db.ArchiveFiles.AddRange(
                        new ArchiveFile
                        {
                            Name = "عقد_توريد_برمجيات_سيرفرات_2026.pdf",
                            StoredName = "sample/contract_01.pdf",
                            Extension = ".pdf",
                            ContentType = "application/pdf",
                            SizeBytes = 1024 * 1024 + 400 * 1024, // 1.4 MB
                            DocumentType = DocumentType.Contract,
                            FolderId = f1.Id,
                            CategoryId = catLegal?.Id,
                            DepartmentId = legal.Id,
                            ReferenceNo = "CNT-2026-0089",
                            Party = "شركة السحاب التقنية",
                            DocumentDate = DateTime.UtcNow.AddMonths(-2),
                            ExpiryDate = DateTime.UtcNow.AddMonths(10),
                            Amount = 145000,
                            Currency = "SAR",
                            Description = "عقد توريد وتشغيل خوادم سحابية مخصص لنظام الأرشفة",
                            CreatedBy = "admin",
                            CreatedAt = DateTime.UtcNow.AddDays(-10),
                            IsFavorite = true
                        },
                        new ArchiveFile
                        {
                            Name = "فاتورة_ضريبية_INV_4091.pdf",
                            StoredName = "sample/inv_4091.pdf",
                            Extension = ".pdf",
                            ContentType = "application/pdf",
                            SizeBytes = 320 * 1024,
                            DocumentType = DocumentType.Invoice,
                            FolderId = f2.Id,
                            CategoryId = catFin?.Id,
                            DepartmentId = finance.Id,
                            ReferenceNo = "INV-2026-4091",
                            Party = "مؤسسة الأفق للحلول",
                            DocumentDate = DateTime.UtcNow.AddDays(-5),
                            ExpiryDate = DateTime.UtcNow.AddDays(25),
                            Amount = 24650,
                            Currency = "SAR",
                            Description = "فاتورة دورية لخدمات الصيانة والدعم الفني",
                            CreatedBy = "finance_user",
                            CreatedAt = DateTime.UtcNow.AddDays(-5),
                            IsFavorite = false
                        },
                        new ArchiveFile
                        {
                            Name = "قرار_إداري_تعيين_مشرفين_2026.docx",
                            StoredName = "sample/decision_2026.docx",
                            Extension = ".docx",
                            ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                            SizeBytes = 84 * 1024,
                            DocumentType = DocumentType.Official,
                            FolderId = f3.Id,
                            CategoryId = catHR?.Id,
                            DepartmentId = executive.Id,
                            ReferenceNo = "DEC-2026-112",
                            Party = "الإدارة العامة",
                            DocumentDate = DateTime.UtcNow.AddDays(-14),
                            Description = "قرار التشكيل الإداري لمشرفي الأقسام وتحديد الصلاحيات",
                            CreatedBy = "admin",
                            CreatedAt = DateTime.UtcNow.AddDays(-14),
                            IsFavorite = true
                        },
                        new ArchiveFile
                        {
                            Name = "خطاب_تنسيق_وزارة_الموارد_البشرية.pdf",
                            StoredName = "sample/corr_hr_01.pdf",
                            Extension = ".pdf",
                            ContentType = "application/pdf",
                            SizeBytes = 512 * 1024,
                            DocumentType = DocumentType.Correspondence,
                            FolderId = f4.Id,
                            CategoryId = catHR?.Id,
                            DepartmentId = hr.Id,
                            ReferenceNo = "COR-2026-0312",
                            Party = "وزارة الموارد البشرية",
                            DocumentDate = DateTime.UtcNow.AddDays(-3),
                            Description = "خطاب صادر بشأن تحديث نسب التوطين واللوائح الوظيفية",
                            CreatedBy = "archivist",
                            CreatedAt = DateTime.UtcNow.AddDays(-3),
                            IsFavorite = false
                        }
                    );
                }

                // Initial Activity Logs
                if (!await _db.ActivityLogs.AnyAsync())
                {
                    _db.ActivityLogs.AddRange(
                        new ActivityLog { Action = "إنشاء نظام", ItemType = "System", ItemName = "نظام الأرشفة", Details = "تهيئة النظام وقاعدة البيانات وتأسيس الأقسام والتصنيفات", UserName = "admin" },
                        new ActivityLog { Action = "رفع ملف", ItemType = "File", ItemName = "عقد_توريد_برمجيات_سيرفرات_2026.pdf", Details = "تم رفع عقد رسمي برقم مرجعي CNT-2026-0089", UserName = "admin" },
                        new ActivityLog { Action = "إنشاء مجلد", ItemType = "Folder", ItemName = "العقود الرسمية 2026", Details = "إنشاء مجلد جديد في قسم الشؤون القانونية", UserName = "admin" }
                    );
                }
            }

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "حدث خطأ أثناء تهيئة البيانات الأولية لقاعدة البيانات.");
        }
    }
}

