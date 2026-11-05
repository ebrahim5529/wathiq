using System.ComponentModel.DataAnnotations;

namespace HR.Models;

public class ActivityLog
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string UserName { get; set; } = "النظام";

    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty; // Upload, Download, Delete, Restore, Share, Rename, CreateFolder, Update, SettingsChange

    [Required, MaxLength(50)]
    public string ItemType { get; set; } = string.Empty; // File, Folder, Category, Department, Setting, User

    public int? ItemId { get; set; }

    [MaxLength(255)]
    public string ItemName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Details { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

