using HR.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

public class SharedController : Controller
{
    private readonly ApplicationDbContext _db;

    public SharedController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var shares = await _db.ArchiveShares
            .Include(s => s.File)
                .ThenInclude(f => f!.Department)
            .Include(s => s.Folder)
                .ThenInclude(f => f!.Department)
            .Include(s => s.SharedWithDepartment)
            .OrderByDescending(s => s.SharedAt)
            .ToListAsync();

        return View(shares);
    }
}
