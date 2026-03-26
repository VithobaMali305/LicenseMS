using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Controllers;

public class DashboardController : Controller
{
    private readonly GatewayClient _gateway;

    public DashboardController(GatewayClient gateway) => _gateway = gateway;

    // ── Admin Dashboard ───────────────────────────────────────────────────
    // GET /Dashboard/AdminDashboard
    [HttpGet]
    public async Task<IActionResult> AdminDashboard(string? status)
    {
        if (!IsAdmin()) return Forbid();

        var stats    = await _gateway.GetStatsAsync();
        var licenses = await _gateway.GetAllLicensesAsync(status);

        var vm = new AdminDashboardViewModel
        {
            TotalUsers    = stats?.TotalUsers    ?? 0,
            TotalLicenses = stats?.TotalLicenses ?? 0,
            PendingCount  = stats?.PendingCount  ?? 0,
            ApprovedCount = stats?.ApprovedCount ?? 0,
            RejectedCount = stats?.RejectedCount ?? 0,
            Licenses      = licenses,
            StatusFilter  = status
        };

        return View(vm);
    }

    // POST /Dashboard/UpdateStatus
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(UpdateStatusViewModel model)
    {
        if (!IsAdmin()) return Forbid();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Invalid request.";
            return RedirectToAction(nameof(AdminDashboard));
        }

        var (ok, error) = await _gateway.UpdateStatusAsync(
            model.LicenseId, model.NewStatus, model.ReviewNotes);

        TempData[ok ? "Success" : "Error"] = ok
            ? $"License #{model.LicenseId} updated to {model.NewStatus}."
            : error ?? "Update failed.";

        return RedirectToAction(nameof(AdminDashboard));
    }

    // ── User Dashboard ────────────────────────────────────────────────────
    // GET /Dashboard/UserDashboard
    [HttpGet]
    public async Task<IActionResult> UserDashboard()
    {
        if (!IsAuthenticated()) return RedirectToLogin();

        var licenses = await _gateway.GetMyLicensesAsync();
        var vm = new UserDashboardViewModel
        {
            Username = HttpContext.Session.GetString("username") ?? "",
            Licenses = licenses
        };

        return View(vm);
    }

    // ── Apply License ─────────────────────────────────────────────────────
    // GET /Dashboard/Apply
    [HttpGet]
    public IActionResult Apply()
    {
        if (!IsAuthenticated()) return RedirectToLogin();
        return View(new ApplyLicenseViewModel());
    }

    // POST /Dashboard/Apply
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(ApplyLicenseViewModel model)
    {
        if (!IsAuthenticated()) return RedirectToLogin();
        if (!ModelState.IsValid) return View(model);

        // Step 1: Upload document
        var (uploadOk, filePath, uploadError) =
            await _gateway.UploadDocumentAsync(model.Document!);

        if (!uploadOk)
        {
            ModelState.AddModelError("Document", uploadError ?? "Upload failed.");
            return View(model);
        }

        // Step 2: Submit license application
        var userId = HttpContext.Session.GetInt32("userId") ?? 0;
        var (applyOk, licenseId, applyError) =
            await _gateway.ApplyLicenseAsync(userId, model.LicenseType, filePath);

        if (!applyOk)
        {
            ModelState.AddModelError(string.Empty, applyError ?? "Application failed.");
            return View(model);
        }

        TempData["Success"] =
            $"License application #{licenseId} submitted successfully! Status: Pending.";
        return RedirectToAction(nameof(UserDashboard));
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private bool IsAuthenticated() =>
        !string.IsNullOrEmpty(HttpContext.Session.GetString("username"));

    private bool IsAdmin() =>
        HttpContext.Session.GetString("role") == "Admin";

    private IActionResult RedirectToLogin() =>
        RedirectToAction("Login", "Account",
            new { returnUrl = Request.Path.Value });
}
