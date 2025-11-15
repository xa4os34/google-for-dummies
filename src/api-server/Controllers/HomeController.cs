using Microsoft.AspNetCore.Mvc;

namespace Api_server.Controllers;

public class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        return View();
    }
}

