using Microsoft.AspNetCore.Mvc;

namespace HR.Controllers;

public class FavoritesController : Controller
{
    public IActionResult Index() => View();
}
