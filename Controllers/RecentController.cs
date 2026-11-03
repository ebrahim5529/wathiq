using Microsoft.AspNetCore.Mvc;

namespace HR.Controllers;

public class RecentController : Controller
{
    public IActionResult Index() => View();
}
