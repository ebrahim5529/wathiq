using Microsoft.AspNetCore.Mvc;

namespace HR.Controllers;

public class UsersController : Controller
{
    public IActionResult Index() => View();
}
