using Microsoft.AspNetCore.Mvc;

namespace HR.Controllers;

public class DashboardController : Controller
{
    public IActionResult Index() => View();
}
