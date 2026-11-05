using HR.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

public class RecentController : Controller
{
    private readonly ApplicationDbContext _db;

    public RecentController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var recentFiles = await _db.ArchiveFiles
            .Include(f => f.Category)
            .Include(f => f.Department)
            .Include(f => f.Folder)
            .Where(f => !f.IsDeleted)
            .OrderByDescending(f => f.UpdatedAt ?? f.CreatedAt)
            .Take(50)
            .ToListAsync();

        return View(recentFiles);
    }
}
