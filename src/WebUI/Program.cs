using Serilog;
using WebUI.Services;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
builder.Host.UseSerilog();

// ── Session ───────────────────────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opts =>
{
    opts.IdleTimeout    = TimeSpan.FromHours(8);
    opts.Cookie.HttpOnly = true;
    opts.Cookie.IsEssential = true;
});

// ── HTTP Client → Gateway ─────────────────────────────────────────────────
builder.Services.AddHttpClient("Gateway", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Gateway:BaseUrl"] ?? "http://localhost:5000");
});

// ── Services ──────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<GatewayClient>();

// ── MVC + AntiForgery ─────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(opts => opts.Cookie.Name = "XSRF-TOKEN");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// Custom error pages
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
