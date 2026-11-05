using HR.Models;

namespace HR.Services;

public interface IStorageService
{
    Task<(string storedName, long sizeBytes, string contentType, string extension)> SaveFileAsync(IFormFile file);
    (Stream Stream, string ContentType, string DownloadName)? GetFileDownload(ArchiveFile file);
    bool DeletePhysicalFile(string storedName);
    Task<(long usedBytes, long quotaBytes, double percentage)> GetStorageUsageAsync(long dbTotalBytes);
    Task<(Stream stream, string fileName)> CreateZipFromFilesAsync(IEnumerable<ArchiveFile> files, string zipBaseName);
}

