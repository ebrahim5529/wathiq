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
    }
}
