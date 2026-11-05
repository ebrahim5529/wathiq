using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR.Models;

public class ArchiveFile
{
    public int Id { get; set; }

    public int? FolderId { get; set; }
    public ArchiveFolder? Folder { get; set; }

    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string StoredName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Extension { get; set; }

    [MaxLength(150)]
    public string? ContentType { get; set; }

    public long SizeBytes { get; set; }

    public DocumentType DocumentType { get; set; } = DocumentType.General;

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    [MaxLength(100)]
    public string? ReferenceNo { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(255)]
    public string? Party { get; set; }

    public DateTime? DocumentDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Amount { get; set; }

    [MaxLength(20)]
    public string? Currency { get; set; } = "SAR";

    [MaxLength(255)]
    public string? Tags { get; set; }

    public bool IsFavorite { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<ArchiveShare> Shares { get; set; } = new List<ArchiveShare>();
}
