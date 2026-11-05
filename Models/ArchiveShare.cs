using System.ComponentModel.DataAnnotations;

namespace HR.Models;

public class ArchiveShare
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string ItemType { get; set; } = "File"; // "File" or "Folder"

    public int? FileId { get; set; }
    public ArchiveFile? File { get; set; }

    public int? FolderId { get; set; }
    public ArchiveFolder? Folder { get; set; }

    [MaxLength(100)]
    public string? SharedWithUser { get; set; }

    public int? SharedWithDepartmentId { get; set; }
    public Department? SharedWithDepartment { get; set; }

    [Required, MaxLength(30)]
    public string Permission { get; set; } = "View"; // "View", "Download", "Edit"

    [MaxLength(100)]
    public string? SharedBy { get; set; }

    public DateTime SharedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Notes { get; set; }
}

