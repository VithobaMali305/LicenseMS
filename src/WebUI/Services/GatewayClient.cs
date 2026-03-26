using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WebUI.Models;

namespace WebUI.Services;

/// <summary>
/// Thin HTTP client wrapper for communicating with the API Gateway.
/// All methods attach the JWT Bearer token from the current session.
/// </summary>
public class GatewayClient
{
    private readonly IHttpClientFactory _factory;
    private readonly IHttpContextAccessor _ctx;
    private readonly ILogger<GatewayClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GatewayClient(IHttpClientFactory factory,
                         IHttpContextAccessor ctx,
                         ILogger<GatewayClient> logger)
    {
        _factory = factory;
        _ctx     = ctx;
        _logger  = logger;
    }

    // ── Auth ──────────────────────────────────────────────────────────────
    public async Task<(bool ok, string? token, string? role, string? username, int userId, string? error)>
        LoginAsync(string username, string password)
    {
        var client = _factory.CreateClient("Gateway");
        var body   = JsonContent(new { username, password });
        var resp   = await client.PostAsync("/api/auth/login", body);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await ReadErrorAsync(resp);
            return (false, null, null, null, 0, err);
        }
        var data = await Deserialize<LoginResult>(resp);
        return (true, data?.Token, data?.Role, data?.Username, data?.UserId ?? 0, null);
    }

    public async Task<(bool ok, string? error)> RegisterAsync(string username, string password, string confirm)
    {
        var client = _factory.CreateClient("Gateway");
        var body   = JsonContent(new { username, password, confirmPassword = confirm });
        var resp   = await client.PostAsync("/api/auth/register", body);
        if (!resp.IsSuccessStatusCode)
            return (false, await ReadErrorAsync(resp));
        return (true, null);
    }

    // ── Licenses ──────────────────────────────────────────────────────────
    public async Task<List<LicenseViewModel>> GetMyLicensesAsync()
    {
        var client = AuthorizedClient();
        var resp   = await client.GetAsync("/api/license/my");
        resp.EnsureSuccessStatusCode();
        return await Deserialize<List<LicenseViewModel>>(resp) ?? new();
    }

    public async Task<List<LicenseViewModel>> GetAllLicensesAsync(string? statusFilter)
    {
        var client = AuthorizedClient();
        var url    = string.IsNullOrEmpty(statusFilter)
            ? "/api/license/all"
            : $"/api/license/all?status={Uri.EscapeDataString(statusFilter)}";
        var resp = await client.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        return await Deserialize<List<LicenseViewModel>>(resp) ?? new();
    }

    public async Task<AdminStatsResult?> GetStatsAsync()
    {
        var client = AuthorizedClient();
        var resp   = await client.GetAsync("/api/license/stats");
        resp.EnsureSuccessStatusCode();
        return await Deserialize<AdminStatsResult>(resp);
    }

    public async Task<(bool ok, int licenseId, string? error)>
        ApplyLicenseAsync(int userId, string licenseType, string? documentPath)
    {
        var client = AuthorizedClient();
        var body   = JsonContent(new { userId, licenseType, documentPath });
        var resp   = await client.PostAsync("/api/license/apply", body);
        if (!resp.IsSuccessStatusCode)
            return (false, 0, await ReadErrorAsync(resp));
        var data = await Deserialize<ApplyResult>(resp);
        return (true, data?.LicenseId ?? 0, null);
    }

    public async Task<(bool ok, string? error)>
        UpdateStatusAsync(int licenseId, string newStatus, string? reviewNotes)
    {
        var client = AuthorizedClient();
        var body   = JsonContent(new { newStatus, reviewNotes });
        var resp   = await client.PutAsync($"/api/license/{licenseId}/status", body);
        if (!resp.IsSuccessStatusCode)
            return (false, await ReadErrorAsync(resp));
        return (true, null);
    }

    // ── Document Upload ───────────────────────────────────────────────────
    public async Task<(bool ok, string? filePath, string? error)>
        UploadDocumentAsync(IFormFile file)
    {
        var client  = AuthorizedClient();
        using var content = new MultipartFormDataContent();
        using var stream  = file.OpenReadStream();
        content.Add(new StreamContent(stream), "file", file.FileName);

        var resp = await client.PostAsync("/api/upload/document", content);
        if (!resp.IsSuccessStatusCode)
            return (false, null, await ReadErrorAsync(resp));

        var data = await Deserialize<UploadResult>(resp);
        return (true, data?.FilePath, null);
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private HttpClient AuthorizedClient()
    {
        var client = _factory.CreateClient("Gateway");
        var token  = _ctx.HttpContext?.Request.Cookies["jwt_token"];
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static StringContent JsonContent(object obj)
        => new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    private static async Task<T?> Deserialize<T>(HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage resp)
    {
        try
        {
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                return msg.GetString() ?? resp.ReasonPhrase ?? "Unknown error";
        }
        catch { }
        return resp.ReasonPhrase ?? "Unknown error";
    }

    // ── Result records ────────────────────────────────────────────────────
    private record LoginResult(string Token, string Role, string Username, int UserId);
    private record ApplyResult(int LicenseId, string Message);
    private record UploadResult(string FilePath);
    public  record AdminStatsResult(int TotalUsers, int TotalLicenses,
                                    int PendingCount, int ApprovedCount, int RejectedCount);
}
