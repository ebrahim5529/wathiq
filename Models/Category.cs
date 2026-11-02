using System.ComponentModel.DataAnnotations;

namespace HR.Models;

public class Category
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(30)]
    public string? Color { get; set; }

    [MaxLength(50)]
    public string? Icon { get; set; }

    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();

    public bool IsActive { get; set; } = true;

    public ICollection<ArchiveFile> Files { get; set; } = new List<ArchiveFile>();
}
