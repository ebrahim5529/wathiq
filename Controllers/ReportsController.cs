using Microsoft.AspNetCore.Mvc;

namespace HR.Controllers;

public class ReportsController : Controller
{
    public IActionResult Statistics() => View();

    public IActionResult Activity() => View();

    public IActionResult Storage() => View();
}
