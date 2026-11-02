using System.ComponentModel.DataAnnotations;

namespace HR.Models;

public class ArchiveFolder
{
    public int Id { get; set; }

    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public int? ParentId { get; set; }
    public ArchiveFolder? Parent { get; set; }
    public ICollection<ArchiveFolder> Children { get; set; } = new List<ArchiveFolder>();

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<ArchiveFile> Files { get; set; } = new List<ArchiveFile>();
}
