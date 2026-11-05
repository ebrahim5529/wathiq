using HR.Models;

namespace HR.ViewModels;

public class FileManagerViewModel
{
    public ArchiveFolder? CurrentFolder { get; set; }
    public List<ArchiveFolder> Breadcrumbs { get; set; } = new();
    public List<ArchiveFolder> Folders { get; set; } = new();
    public List<ArchiveFile> Files { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Department> Departments { get; set; } = new();

    public int? CurrentFolderId => CurrentFolder?.Id;
    public DocumentType? FilterDocumentType { get; set; }
    public string? ActiveSection { get; set; } // "all", "official", "contract", "correspondence", "invoice", "favorites", "recent", "shared", "trash"

    public int TotalItemsCount => Folders.Count + Files.Count;
    public long StorageUsedBytes { get; set; }
    public long StorageQuotaBytes { get; set; }
    public double StoragePercentage { get; set; }

    public string Title { get; set; } = "مدير الملفات";
}

public class DashboardViewModel
{
    public int TotalFiles { get; set; }
    public int TotalFolders { get; set; }
    public int TotalCategories { get; set; }
    public int TotalDepartments { get; set; }
    public int FilesUploadedToday { get; set; }
    public int FilesUploadedThisWeek { get; set; }

    public long StorageUsedBytes { get; set; }
    public long StorageQuotaBytes { get; set; }
    public double StoragePercentage { get; set; }

    public Dictionary<DocumentType, int> DocumentTypeDistribution { get; set; } = new();
    public Dictionary<string, int> DepartmentDistribution { get; set; } = new();
    public List<ArchiveFile> RecentFiles { get; set; } = new();
    public List<ArchiveFile> ExpiringContracts { get; set; } = new();
    public List<ActivityLog> RecentActivityLogs { get; set; } = new();
}

public class SearchFilterViewModel
{
    public string? Query { get; set; }
    public string? ReferenceNo { get; set; }
    public DocumentType? DocumentType { get; set; }
    public int? CategoryId { get; set; }
    public int? DepartmentId { get; set; }
    public string? Extension { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? CreatedBy { get; set; }
    public bool IncludeDeleted { get; set; }

    public List<ArchiveFile> Results { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Department> Departments { get; set; } = new();
    public bool HasSearched { get; set; }
}

public class StorageReportViewModel
{
    public long TotalUsedBytes { get; set; }
    public long QuotaBytes { get; set; }
    public double Percentage { get; set; }

    public List<DepartmentStorageStat> DepartmentStats { get; set; } = new();
    public List<CategoryStorageStat> CategoryStats { get; set; } = new();
    public List<FileTypeStat> FileTypeStats { get; set; } = new();
}

public class DepartmentStorageStat
{
    public string DepartmentName { get; set; } = string.Empty;
    public int FilesCount { get; set; }
    public long SizeBytes { get; set; }
    public double PercentageOfTotal { get; set; }
}

public class CategoryStorageStat
{
    public string CategoryName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int FilesCount { get; set; }
    public long SizeBytes { get; set; }
}

public class FileTypeStat
{
    public string Extension { get; set; } = string.Empty;
    public int Count { get; set; }
    public long SizeBytes { get; set; }
}

