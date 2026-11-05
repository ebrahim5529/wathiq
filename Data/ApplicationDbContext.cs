using HR.Models;
using Microsoft.EntityFrameworkCore;

namespace HR.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ArchiveFolder> ArchiveFolders => Set<ArchiveFolder>();
    public DbSet<ArchiveFile> ArchiveFiles => Set<ArchiveFile>();
    public DbSet<ArchiveShare> ArchiveShares => Set<ArchiveShare>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(x => x.Name);
            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ArchiveFolder>(entity =>
        {
            entity.HasIndex(x => new { x.ParentId, x.Name });
            entity.HasIndex(x => x.IsDeleted);
            entity.HasIndex(x => x.IsFavorite);
            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Department)
                .WithMany(x => x.Folders)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ArchiveFile>(entity =>
        {
            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.DocumentType);
            entity.HasIndex(x => x.ReferenceNo);
            entity.HasIndex(x => x.IsDeleted);
            entity.HasIndex(x => x.IsFavorite);
            entity.HasOne(x => x.Folder)
                .WithMany(x => x.Files)
                .HasForeignKey(x => x.FolderId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Category)
                .WithMany(x => x.Files)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Department)
                .WithMany(x => x.Files)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ArchiveShare>(entity =>
        {
            entity.HasIndex(x => new { x.ItemType, x.FileId, x.FolderId });
            entity.HasOne(x => x.File)
                .WithMany(x => x.Shares)
                .HasForeignKey(x => x.FileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Folder)
                .WithMany(x => x.Shares)
                .HasForeignKey(x => x.FolderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.SharedWithDepartment)
                .WithMany()
                .HasForeignKey(x => x.SharedWithDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.Action);
            entity.HasIndex(x => x.UserName);
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
        });
    }
}
