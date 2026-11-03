using Microsoft.AspNetCore.Mvc;

namespace HR.Controllers;

public class SettingsController : Controller
{
    public IActionResult Index() => View();
}
