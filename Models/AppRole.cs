using System.ComponentModel.DataAnnotations;

namespace HR.Models;

public class AppRole
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty; // Admin, Archivist, DeptSupervisor, Employee, Guest

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public string Permissions { get; set; } = string.Empty; // Comma-separated or JSON list of permissions
}

