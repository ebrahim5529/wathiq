using HR.Data;
using HR.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

public class FavoritesController : Controller
{
    private readonly ApplicationDbContext _db;

    public FavoritesController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var favoriteFolders = await _db.ArchiveFolders
            .Include(f => f.Department)
            .Include(f => f.Files)
            .Where(f => !f.IsDeleted && f.IsFavorite)
            .OrderBy(f => f.Name)
            .ToListAsync();

        var favoriteFiles = await _db.ArchiveFiles
            .Include(f => f.Category)
            .Include(f => f.Department)
            .Where(f => !f.IsDeleted && f.IsFavorite)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        ViewBag.FavoriteFolders = favoriteFolders;
        return View(favoriteFiles);
    }
}
