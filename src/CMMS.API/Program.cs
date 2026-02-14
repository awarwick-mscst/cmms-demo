using System.Text;
using CMMS.API.Middleware;
using CMMS.API.Services;
using CMMS.Core.Configuration;
using CMMS.Core.Interfaces;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Repositories;
using CMMS.Infrastructure.Services;
using CMMS.Infrastructure.Services.Providers;
using CMMS.Shared.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// When running as a Windows Service, the working directory defaults to System32.
// Set it to the exe's directory so config files and wwwroot are found correctly.
if (WindowsServiceHelpers.IsWindowsService())
{
    Directory.SetCurrentDirectory(AppContext.BaseDirectory);
}

var builder = WebApplication.CreateBuilder(args);

// Enable running as a Windows Service (no-op in console/dev mode)
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "CmmsService";
});

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/cmms-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add DbContext with dynamic provider selection
var dbSettings = builder.Configuration.GetSection(CMMS.Core.Configuration.DatabaseSettings.SectionName)
    .Get<CMMS.Core.Configuration.DatabaseSettings>();
var dbProvider = dbSettings?.Provider ?? CMMS.Core.Configuration.DatabaseProvider.SqlServer;
SqlDialect.Provider = dbProvider;

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

builder.Services.AddDbContext<CmmsDbContext>(options =>
{
    switch (dbProvider)
    {
        case CMMS.Core.Configuration.DatabaseProvider.PostgreSql:
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(3);
                npgsqlOptions.CommandTimeout(30);
            });
            break;
        default:
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(3);
                sqlOptions.CommandTimeout(30);
            });
            break;
    }
});

// Add repositories and services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IAssetCategoryService, AssetCategoryService>();
builder.Services.AddScoped<IAssetLocationService, AssetLocationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IPartCategoryService, PartCategoryService>();
builder.Services.AddScoped<IStorageLocationService, StorageLocationService>();
builder.Services.AddScoped<IPartService, PartService>();
builder.Services.AddScoped<IWorkOrderService, WorkOrderService>();
builder.Services.AddScoped<IWorkOrderTaskService, WorkOrderTaskService>();
builder.Services.AddScoped<IWorkOrderTaskTemplateService, WorkOrderTaskTemplateService>();
builder.Services.AddScoped<IPreventiveMaintenanceService, PreventiveMaintenanceService>();
builder.Services.AddScoped<ILabelService, LabelService>();
builder.Services.AddScoped<IPrintService, PrintService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IFileStorageService>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var logger = sp.GetRequiredService<ILogger<FileStorageService>>();
    return new FileStorageService(env.WebRootPath, logger);
});
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddSingleton<IDatabaseConfigService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<DatabaseConfigService>>();
    return new DatabaseConfigService(logger);
});
builder.Services.AddSingleton<IUpdateService, UpdateService>();

// Configure AI Assistant settings
builder.Services.Configure<AiAssistantSettings>(builder.Configuration.GetSection(AiAssistantSettings.SectionName));
var aiSettings = builder.Configuration.GetSection(AiAssistantSettings.SectionName).Get<AiAssistantSettings>() ?? new AiAssistantSettings();

if (aiSettings.Enabled)
{
    builder.Services.AddHttpClient("AiAssistant", client =>
    {
        client.BaseAddress = new Uri(aiSettings.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(aiSettings.TimeoutSeconds);
    });
    builder.Services.AddScoped<IAiAssistantService, AiAssistantService>();
    Log.Information("AI Assistant enabled - Endpoint: {Url}, Model: {Model}", aiSettings.BaseUrl, aiSettings.Model);
}
else
{
    Log.Information("AI Assistant disabled");
}

// Configure Licensing settings
builder.Services.Configure<LicensingSettings>(builder.Configuration.GetSection(LicensingSettings.SectionName));
var licensingSettings = builder.Configuration.GetSection(LicensingSettings.SectionName).Get<LicensingSettings>() ?? new LicensingSettings();

if (licensingSettings.Enabled)
{
    builder.Services.AddHttpClient("LicenseServer", client =>
    {
        client.BaseAddress = new Uri(licensingSettings.LicenseServerUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    });
    builder.Services.AddScoped<ILicenseService, LicenseService>();
    builder.Services.AddHostedService<LicensePhoneHomeService>();
    Log.Information("Licensing system enabled - Server: {Url}", licensingSettings.LicenseServerUrl);
}
else
{
    builder.Services.AddSingleton<ILicenseService, UnlicensedModeService>();
    Log.Information("Licensing system disabled - all features unlocked");
}

// Configure Email/Calendar settings
builder.Services.Configure<EmailCalendarSettings>(builder.Configuration.GetSection(EmailCalendarSettings.SectionName));
var emailCalendarSettings = builder.Configuration.GetSection(EmailCalendarSettings.SectionName).Get<EmailCalendarSettings>() ?? new EmailCalendarSettings();

// Register HttpClientFactory for Teams and other HTTP-based providers
builder.Services.AddHttpClient("Teams", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register notification and calendar services
builder.Services.AddScoped<IIntegrationSettingsService, IntegrationSettingsService>();
builder.Services.AddScoped<IEmailProvider, MicrosoftGraphEmailProvider>();
builder.Services.AddScoped<ICalendarProvider, MicrosoftGraphCalendarProvider>();
builder.Services.AddScoped<ITeamsProvider, MicrosoftGraphTeamsProvider>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICalendarSyncService, CalendarSyncService>();

// Register background services for notifications
if (emailCalendarSettings.Enabled)
{
    builder.Services.AddHostedService<NotificationBackgroundService>();
    builder.Services.AddHostedService<NotificationSchedulerService>();
    Log.Information("Notification system enabled - Background services registered");
}
else
{
    Log.Information("Notification system disabled");
}

// Configure LDAP settings
builder.Services.Configure<LdapSettings>(builder.Configuration.GetSection(LdapSettings.SectionName));
var ldapSettings = builder.Configuration.GetSection(LdapSettings.SectionName).Get<LdapSettings>() ?? new LdapSettings();

// Register LDAP service - use NullLdapService when disabled for better performance
if (ldapSettings.Enabled)
{
    builder.Services.AddScoped<ILdapService, LdapService>();
    Log.Information("LDAP authentication enabled - Server: {Server}, Mode: {Mode}",
        ldapSettings.Server, ldapSettings.AuthenticationMode);
}
else
{
    builder.Services.AddSingleton<ILdapService, NullLdapService>();
    Log.Information("LDAP authentication disabled - using local authentication only");
}

// Register AuthService (depends on ILdapService)
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddHttpContextAccessor();

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// Add Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "CMMS",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "CMMS-Users",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Add Authorization policies
builder.Services.AddAuthorization(options =>
{
    // Asset policies
    options.AddPolicy("CanViewAssets", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "assets.view") ||
            context.User.IsInRole("Administrator")));

    options.AddPolicy("CanCreateAssets", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "assets.create") ||
            context.User.IsInRole("Administrator")));

    options.AddPolicy("CanEditAssets", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "assets.edit") ||
            context.User.IsInRole("Administrator")));

    options.AddPolicy("CanDeleteAssets", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "assets.delete") ||
            context.User.IsInRole("Administrator")));

    // Asset Category policies
    options.AddPolicy("CanCreateAssetCategories", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "asset-categories.create") ||
            context.User.IsInRole("Administrator")));

    options.AddPolicy("CanEditAssetCategories", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "asset-categories.edit") ||
            context.User.IsInRole("Administrator")));

    options.AddPolicy("CanDeleteAssetCategories", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "asset-categories.delete") ||
            context.User.IsInRole("Administrator")));

    // Asset Location policies
    options.AddPolicy("CanCreateAssetLocations", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "asset-locations.create") ||
            context.User.IsInRole("Administrator")));

    options.AddPolicy("CanEditAssetLocations", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "asset-locations.edit") ||
            context.User.IsInRole("Administrator")));

    options.AddPolicy("CanDeleteAssetLocations", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "asset-locations.delete") ||
            context.User.IsInRole("Administrator")));

    // Inventory policies
    options.AddPolicy("CanManageInventory", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "inventory.manage") ||
            context.User.IsInRole("Administrator") ||
            context.User.IsInRole("Inventory Manager")));

    // Work Order policies
    options.AddPolicy("CanViewWorkOrders", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "work-orders.view") ||
            context.User.IsInRole("Administrator") ||
            context.User.IsInRole("Technician")));

    options.AddPolicy("CanCreateWorkOrders", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "work-orders.create") ||
            context.User.IsInRole("Administrator") ||
            context.User.IsInRole("Technician")));

    options.AddPolicy("CanEditWorkOrders", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "work-orders.edit") ||
            context.User.IsInRole("Administrator") ||
            context.User.IsInRole("Technician")));

    options.AddPolicy("CanDeleteWorkOrders", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "work-orders.delete") ||
            context.User.IsInRole("Administrator")));

    // Preventive Maintenance policies
    options.AddPolicy("CanViewPreventiveMaintenance", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "preventive-maintenance.view") ||
            context.User.IsInRole("Administrator") ||
            context.User.IsInRole("Technician")));

    options.AddPolicy("CanManagePreventiveMaintenance", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "preventive-maintenance.manage") ||
            context.User.IsInRole("Administrator")));

    // User Management policies
    options.AddPolicy("CanViewUsers", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "users.view") ||
            context.User.IsInRole("Administrator")));

    options.AddPolicy("CanManageUsers", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "users.manage") ||
            context.User.IsInRole("Administrator")));

    // Label Printing policies
    options.AddPolicy("CanPrintLabels", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "labels.print") ||
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "inventory.manage") ||
            context.User.IsInRole("Administrator") ||
            context.User.IsInRole("Inventory Manager")));

    options.AddPolicy("CanManageLabels", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "permission" && c.Value == "labels.manage") ||
            context.User.IsInRole("Administrator")));
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://fragbox:3000" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add controllers
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CMMS API",
        Version = "v1",
        Description = "Computerized Maintenance Management System API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Auto-migrate database in non-Development environments (production installs / updates)
if (!app.Environment.IsDevelopment())
{
    using var migrationScope = app.Services.CreateScope();
    var migrationDb = migrationScope.ServiceProvider.GetRequiredService<CmmsDbContext>();
    migrationDb.Database.Migrate();
    Log.Information("Database migrations applied successfully");
}

// Seed admin user if not exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CmmsDbContext>();
    var adminUser = db.Users.FirstOrDefault(u => u.Username == "admin");
    if (adminUser == null)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        db.Users.Add(new CMMS.Core.Entities.User
        {
            Username = "admin",
            Email = "admin@cmms.local",
            PasswordHash = hash,
            FirstName = "System",
            LastName = "Administrator",
            IsActive = true,
            PasswordChangedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        var newUser = db.Users.First(u => u.Username == "admin");
        db.UserRoles.Add(new CMMS.Core.Entities.UserRole { UserId = newUser.Id, RoleId = 1 });
        db.SaveChanges();
        Console.WriteLine($"Admin user created with hash: {hash}");
    }
    else
    {
        // Update existing admin password
        adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        db.SaveChanges();
        Console.WriteLine($"Admin password updated to: Admin@123");
    }
}

// Configure middleware pipeline
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CMMS API v1");
        c.RoutePrefix = "swagger";
    });
}

// CORS must come before HTTPS redirection to handle preflight OPTIONS requests
app.UseCors("CorsPolicy");

// Only use HTTPS redirection in production to avoid breaking CORS in development
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Serve static files (including uploads)
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<LicenseValidationMiddleware>();
app.MapControllers();

// SPA fallback: serve index.html for any unmatched routes (embedded frontend)
app.MapFallbackToFile("index.html");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
