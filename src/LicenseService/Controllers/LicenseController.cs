using LicenseService.Commands;
using LicenseService.Data;
using LicenseService.DTOs;
using LicenseService.Models;
using LicenseService.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LicenseService.Controllers;

// ════════════════════════════════════════════════════════════════════════════
// AUTH CONTROLLER  →  /api/auth/*
// ════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (req.Password != req.ConfirmPassword)
            return BadRequest(new { message = "Passwords do not match." });

        if (await _db.Users.AnyAsync(u => u.Username == req.Username))
            return Conflict(new { message = "Username already exists." });

        var user = new User
        {
            Username = req.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Registration successful. Please log in." });
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password." });

        var token = GenerateJwt(user);
        return Ok(new LoginResponse(token, user.Role, user.Username, user.Id));
    }

    private string GenerateJwt(User user)
    {
        var secret  = _config["Jwt:Secret"]!;
        var issuer   = _config["Jwt:Issuer"]!;
        var audience = _config["Jwt:Audience"]!;
        var hours    = int.Parse(_config["Jwt:ExpiryHours"] ?? "8");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(issuer, audience, claims,
            expires: DateTime.UtcNow.AddHours(hours), signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// LICENSE CONTROLLER  →  /api/license/*
// ════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/license")]
[Authorize]
public class LicenseController : ControllerBase
{
    private readonly IMediator _mediator;

    public LicenseController(IMediator mediator) => _mediator = mediator;

    // GET /api/license/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMyLicenses()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await _mediator.Send(new GetLicensesByUserQuery(userId));
        return Ok(result);
    }

    // GET /api/license/all   (Admin only)
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var result = await _mediator.Send(new GetAllLicensesAdminQuery(status));
        return Ok(result);
    }

    // GET /api/license/stats   (Admin only)
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _mediator.Send(new GetAdminDashboardStatsQuery());
        return Ok(stats);
    }

    // POST /api/license/apply
    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] ApplyLicenseRequest req)
    {
        var id = await _mediator.Send(new ApplyLicenseCommand(
            req.UserId, req.LicenseType, req.DocumentPath));
        return Ok(new { licenseId = id, message = "License application submitted." });
    }

    // PUT /api/license/{id}/status   (Admin only)
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest req)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var success = await _mediator.Send(new UpdateLicenseStatusCommand(
            id, req.NewStatus, req.ReviewNotes, adminId));

        if (!success) return NotFound(new { message = "License not found or invalid status." });
        return Ok(new { message = $"License {id} updated to {req.NewStatus}." });
    }
}
