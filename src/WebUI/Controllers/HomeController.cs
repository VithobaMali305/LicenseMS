using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers;

public class HomeController : Controller
{
    [Route("Home/Error/{statusCode?}")]
    public IActionResult Error(int? statusCode)
    {
        ViewData["StatusCode"] = statusCode ?? 500;
        return View();
    }
}
