using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Fido2NetLib;
using LicensingServer.Web.Data;
using LicensingServer.Web.Services;
using LicensingServer.Web.Services.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/licensing-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add DbContext
builder.Services.AddDbContext<LicensingDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(3);
            sqlOptions.CommandTimeout(30);
        }));

// Session (needed for MFA partial auth and FIDO2 challenges)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = "CmmsLicensing.Session";
});

// Cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
        options.Cookie.Name = "CmmsLicensing.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // In production with HTTPS, enforce secure cookies
        if (!builder.Environment.IsDevelopment())
        {
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        }
    });

builder.Services.AddAuthorization();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(15);
        opt.PermitLimit = 10; // 10 requests per 15 min per IP
        opt.QueueLimit = 0;
    });
});

// FIDO2
var fido2Config = builder.Configuration.GetSection("Fido2");
builder.Services.AddFido2(options =>
{
    options.ServerDomain = fido2Config["ServerDomain"] ?? "localhost";
    options.ServerName = fido2Config["ServerName"] ?? "CMMS Licensing Server";
    options.Origins = fido2Config.GetSection("Origins").Get<HashSet<string>>()
        ?? new HashSet<string> { "https://localhost:5443", "http://localhost:5100" };
    options.TimestampDriftTolerance = fido2Config.GetValue("TimestampDriftTolerance", 300000);
});

// Security services
builder.Services.AddSingleton<PasswordHashingService>();
builder.Services.AddSingleton<TotpService>();
builder.Services.AddSingleton<PartialAuthCookie>();
builder.Services.AddScoped<Fido2AuthService>();
builder.Services.AddScoped<AdminAuditService>();

// Licensing services
builder.Services.AddSingleton<LicenseKeyGenerator>();
builder.Services.AddScoped<LicenseValidationService>();

// Add Razor Pages and Controllers
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/LoginMfa");
    options.Conventions.AllowAnonymousToPage("/Logout");
    options.Conventions.AllowAnonymousToFolder("/Setup");
});
builder.Services.AddControllers();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LicensingDbContext>();
    db.Database.EnsureCreated();
    Log.Information("Licensing database initialized");
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// First-run setup redirect middleware
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";

    // Skip for API, static files, setup pages, and login pages
    if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Setup", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Login", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Logout", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/_", StringComparison.OrdinalIgnoreCase) ||
        path.Contains('.')) // Static files
    {
        await next();
        return;
    }

    var db = context.RequestServices.GetRequiredService<LicensingDbContext>();
    if (!await db.AdminUsers.AnyAsync())
    {
        context.Response.Redirect("/Setup/InitialSetup");
        return;
    }

    await next();
});

app.MapRazorPages();
app.MapControllers(); // API endpoints remain unauthenticated for CMMS to call

// Redirect root to dashboard
app.MapGet("/", () => Results.Redirect("/Dashboard")).RequireAuthorization();

Log.Information("Licensing Server starting");
app.Run();
