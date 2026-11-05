using HR.Data;
using HR.Models;
using HR.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

public class SearchController : Controller
{
    private readonly ApplicationDbContext _db;

    public SearchController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(SearchFilterViewModel filter)
    {
        filter.Categories = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
        filter.Departments = await _db.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();

        // Check if any filter condition was submitted
        bool hasCondition = !string.IsNullOrWhiteSpace(filter.Query) ||
                            !string.IsNullOrWhiteSpace(filter.ReferenceNo) ||
                            filter.DocumentType.HasValue ||
                            filter.CategoryId.HasValue ||
                            filter.DepartmentId.HasValue ||
                            !string.IsNullOrWhiteSpace(filter.Extension) ||
                            filter.DateFrom.HasValue ||
                            filter.DateTo.HasValue ||
                            !string.IsNullOrWhiteSpace(filter.CreatedBy) ||
                            filter.IncludeDeleted;

        if (hasCondition)
        {
            filter.HasSearched = true;
            var query = _db.ArchiveFiles
                .Include(f => f.Category)
                .Include(f => f.Department)
                .Include(f => f.Folder)
                .AsQueryable();

            if (!filter.IncludeDeleted)
            {
                query = query.Where(f => !f.IsDeleted);
            }

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var q = filter.Query.Trim();
                query = query.Where(f => f.Name.Contains(q) ||
                                         (f.Description != null && f.Description.Contains(q)) ||
                                         (f.Party != null && f.Party.Contains(q)) ||
                                         (f.Tags != null && f.Tags.Contains(q)));
            }

            if (!string.IsNullOrWhiteSpace(filter.ReferenceNo))
            {
                var refNo = filter.ReferenceNo.Trim();
                query = query.Where(f => f.ReferenceNo != null && f.ReferenceNo.Contains(refNo));
            }

            if (filter.DocumentType.HasValue)
            {
                query = query.Where(f => f.DocumentType == filter.DocumentType.Value);
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(f => f.CategoryId == filter.CategoryId.Value);
            }

            if (filter.DepartmentId.HasValue)
            {
                query = query.Where(f => f.DepartmentId == filter.DepartmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Extension))
            {
                var ext = filter.Extension.Trim();
                if (!ext.StartsWith(".")) ext = "." + ext;
                query = query.Where(f => f.Extension != null && f.Extension == ext);
            }

            if (filter.DateFrom.HasValue)
            {
                query = query.Where(f => f.CreatedAt >= filter.DateFrom.Value);
            }

            if (filter.DateTo.HasValue)
            {
                var endOfDay = filter.DateTo.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(f => f.CreatedAt <= endOfDay);
            }

            if (!string.IsNullOrWhiteSpace(filter.CreatedBy))
            {
                var user = filter.CreatedBy.Trim();
                query = query.Where(f => f.CreatedBy != null && f.CreatedBy.Contains(user));
            }

            filter.Results = await query.OrderByDescending(f => f.CreatedAt).Take(100).ToListAsync();
        }

        return View(filter);
    }
}
