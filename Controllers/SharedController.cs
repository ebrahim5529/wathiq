using Microsoft.AspNetCore.Mvc;

namespace HR.Controllers;

public class SharedController : Controller
{
    public IActionResult Index() => View("~/Views/SharedItems/Index.cshtml");
}
