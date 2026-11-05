using System.ComponentModel.DataAnnotations;

namespace HR.Models;

public class SystemSetting
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string Value { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Group { get; set; } = "General"; // General, Storage, Security, Notifications

    [MaxLength(255)]
    public string? Description { get; set; }
}

