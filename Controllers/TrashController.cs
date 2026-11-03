using Microsoft.AspNetCore.Mvc;

namespace HR.Controllers;

public class TrashController : Controller
{
    public IActionResult Index() => View();
}
