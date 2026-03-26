using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Controllers;

public class AccountController : Controller
{
    private readonly GatewayClient _gateway;

    public AccountController(GatewayClient gateway) => _gateway = gateway;

    // GET /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (IsLoggedIn()) return RedirectToDashboard();
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST /Account/Login
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var (ok, token, role, username, userId, error) =
            await _gateway.LoginAsync(model.Username, model.Password);

        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Login failed.");
            return View(model);
        }

        // Store JWT in HttpOnly cookie
        Response.Cookies.Append("jwt_token", token!, new CookieOptions
        {
            HttpOnly = true,
            Secure   = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires  = DateTimeOffset.UtcNow.AddHours(8)
        });

        // Store session info
        HttpContext.Session.SetString("username", username!);
        HttpContext.Session.SetString("role",     role!);
        HttpContext.Session.SetInt32("userId",    userId);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToDashboard();
    }

    // GET /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        if (IsLoggedIn()) return RedirectToDashboard();
        return View();
    }

    // POST /Account/Register
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (ok, error) = await _gateway.RegisterAsync(
            model.Username, model.Password, model.ConfirmPassword);

        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Registration failed.");
            return View(model);
        }

        TempData["Success"] = "Registration successful! Please log in.";
        return RedirectToAction(nameof(Login));
    }

    // POST /Account/Logout
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("jwt_token");
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private bool IsLoggedIn() =>
        !string.IsNullOrEmpty(HttpContext.Session.GetString("username"));

    private IActionResult RedirectToDashboard() =>
        HttpContext.Session.GetString("role") == "Admin"
            ? RedirectToAction("AdminDashboard", "Dashboard")
            : RedirectToAction("UserDashboard",  "Dashboard");
}
