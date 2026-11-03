using HR.Models;

namespace HR.Models.ViewModels;

public class ArchiveIndexViewModel
{
    public ArchiveFolder? CurrentFolder { get; set; }

    /// <summary>كل المجلدات غير المحذوفة (لبناء شجرة القائمة الجانبية).</summary>
    public List<ArchiveFolder> AllFolders { get; set; } = [];

    /// <summary>عدد الملفات غير المحذوفة لكل مجلد (معرّف المجلد → العدد).</summary>
    public Dictionary<int, int> FileCounts { get; set; } = [];

    /// <summary>المجلدات الفرعية للمجلد الحالي.</summary>
    public List<ArchiveFolder> Folders { get; set; } = [];

    /// <summary>الملفات داخل المجلد الحالي (بعد الفلاتر).</summary>
    public List<ArchiveFile> Files { get; set; } = [];

    /// <summary>مسار التنقل من الجذر إلى المجلد الحالي.</summary>
    public List<ArchiveFolder> Breadcrumb { get; set; } = [];

    public DocumentType? DocumentType { get; set; }
    public string? Search { get; set; }

    public long UsedBytes { get; set; }
    public long QuotaBytes { get; set; }
    public long MaxFileBytes { get; set; }
    public List<string> AllowedExtensions { get; set; } = [];

    public int TotalItems { get; set; }

    public int UsedPercent => QuotaBytes <= 0
        ? 0
        : (int)Math.Clamp(UsedBytes * 100.0 / QuotaBytes, 0, 100);
}
