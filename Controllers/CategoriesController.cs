using Microsoft.AspNetCore.Mvc;

namespace HR.Controllers;

public class CategoriesController : Controller
{
    public IActionResult Index() => View();
}
