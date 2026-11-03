using Microsoft.AspNetCore.Mvc;

namespace HR.Controllers;

public class DepartmentsController : Controller
{
    public IActionResult Index() => View();
}
