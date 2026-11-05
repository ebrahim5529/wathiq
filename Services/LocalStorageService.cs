using System.IO.Compression;
using HR.Models;

namespace HR.Services;

public class LocalStorageService : IStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly string _uploadsRoot;
    private readonly long _storageQuotaBytes;
    private readonly HashSet<string> _allowedExtensions;

    public LocalStorageService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;

        var configuredRoot = _configuration.GetValue<string>("Archive:UploadsRoot") ?? "uploads";
        _uploadsRoot = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), configuredRoot);
        _storageQuotaBytes = _configuration.GetValue<long?>("Archive:StorageQuotaBytes") ?? (10L * 1024 * 1024 * 1024); // 10 GB

        var exts = _configuration.GetSection("Archive:AllowedExtensions").Get<string[]>()
            ?? new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".ppt", ".pptx", ".png", ".jpg", ".jpeg", ".zip" };
        _allowedExtensions = new HashSet<string>(exts, StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(_uploadsRoot))
        {
            Directory.CreateDirectory(_uploadsRoot);
        }
    }

    public async Task<(string storedName, long sizeBytes, string contentType, string extension)> SaveFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("الملف فارغ أو غير صالح.");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (_allowedExtensions.Count > 0 && !_allowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException($"امتداد الملف '{ext}' غير مسموح به في النظام.");
        }

        var dateFolder = DateTime.UtcNow.ToString("yyyy/MM");
        var targetDir = Path.Combine(_uploadsRoot, dateFolder.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        var uniquePrefix = Guid.NewGuid().ToString("N");
        var safeFileName = Path.GetFileName(file.FileName);
        // Stored name relative to uploadsRoot
        var relativeStoredName = $"{dateFolder}/{uniquePrefix}_{safeFileName}";
        var fullPhysicalPath = Path.Combine(_uploadsRoot, relativeStoredName.Replace('/', Path.DirectorySeparatorChar));

        using (var stream = new FileStream(fullPhysicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        return (relativeStoredName, file.Length, contentType, ext);
    }

    public (Stream Stream, string ContentType, string DownloadName)? GetFileDownload(ArchiveFile file)
    {
        if (string.IsNullOrWhiteSpace(file.StoredName))
        {
            return null;
        }

        var fullPhysicalPath = Path.Combine(_uploadsRoot, file.StoredName.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPhysicalPath))
        {
            return null;
        }

        var stream = new FileStream(fullPhysicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var contentType = file.ContentType ?? "application/octet-stream";
        var downloadName = string.IsNullOrWhiteSpace(file.Name) ? Path.GetFileName(fullPhysicalPath) : file.Name;

        return (stream, contentType, downloadName);
    }

    public bool DeletePhysicalFile(string storedName)
    {
        if (string.IsNullOrWhiteSpace(storedName)) return false;

        try
        {
            var fullPhysicalPath = Path.Combine(_uploadsRoot, storedName.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPhysicalPath))
            {
                File.Delete(fullPhysicalPath);
                return true;
            }
        }
        catch
        {
            // Ignore error on physical deletion
        }
        return false;
    }

    public Task<(long usedBytes, long quotaBytes, double percentage)> GetStorageUsageAsync(long dbTotalBytes)
    {
        var quota = _storageQuotaBytes > 0 ? _storageQuotaBytes : (10L * 1024 * 1024 * 1024);
        var percentage = quota > 0 ? Math.Round(((double)dbTotalBytes / quota) * 100, 1) : 0;
        return Task.FromResult((dbTotalBytes, quota, percentage));
    }

    public async Task<(Stream stream, string fileName)> CreateZipFromFilesAsync(IEnumerable<ArchiveFile> files, string zipBaseName)
    {
        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var fullPhysicalPath = Path.Combine(_uploadsRoot, file.StoredName.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(fullPhysicalPath))
                {
                    var entry = archive.CreateEntry(file.Name, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    using var fileStream = new FileStream(fullPhysicalPath, FileMode.Open, FileAccess.Read);
                    await fileStream.CopyToAsync(entryStream);
                }
            }
        }

        memoryStream.Position = 0;
        var safeZipName = $"{zipBaseName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";
        return (memoryStream, safeZipName);
    }
}

