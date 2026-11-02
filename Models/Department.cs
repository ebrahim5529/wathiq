using System.ComponentModel.DataAnnotations;

namespace HR.Models;

public class Department
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }

    public int? ParentId { get; set; }
    public Department? Parent { get; set; }
    public ICollection<Department> Children { get; set; } = new List<Department>();

    public bool IsActive { get; set; } = true;

    public ICollection<ArchiveFolder> Folders { get; set; } = new List<ArchiveFolder>();
    public ICollection<ArchiveFile> Files { get; set; } = new List<ArchiveFile>();
}
