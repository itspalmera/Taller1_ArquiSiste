using System.IO.Compression;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using Shortly.Application.Interfaces;
using Shortly.Application.Services;
using Shortly.Endpoints;
using Shortly.Infrastructure;
using Shortly.Infrastructure.Persistence;
using Shortly.Infrastructure.Repositories;
using Shortly.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Configuración de Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddRazorPages();

// OpenAPI
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Shortly API",
            Description = "A URL shortener service with HTTP protocol optimizations.",
            Version = "v1"
        };
        return Task.CompletedTask;
    });
});

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AppDbContext")));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<MemoryCacheTicketStore>();

// =========================================================================
// ITEM 9: Cookie Hardening Audit
// Concepto HTTP/Seguridad:
// - HttpOnly: Bloquea el acceso a la cookie desde scripts JS (mitiga XSS).
// - SameSite=Strict: Evita el envío de la cookie en peticiones cross-site (mitiga CSRF).
// - Secure: Fuerza la transmisión únicamente sobre canales HTTPS cifrados.
// =========================================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Error";
        options.Cookie.Name = "__Host-ShortlySession";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Path = "/";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

builder.Services.AddSingleton<IConfigureOptions<CookieAuthenticationOptions>>(sp =>
{
    var store = sp.GetRequiredService<MemoryCacheTicketStore>();
    return new ConfigureNamedOptions<CookieAuthenticationOptions>(
        CookieAuthenticationDefaults.AuthenticationScheme,
        options => options.SessionStore = store);
});

// =========================================================================
// ITEM 5: Rate Limiting
// Concepto HTTP: Previene ataques de fuerza bruta respondiendo '429 Too Many Requests'
// e incluyendo el encabezado 'Retry-After'.
// =========================================================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsync("Too many attempts. Please try again in 60 seconds.", token);
    };
    options.AddFixedWindowLimiter("login-policy", opt =>
    {
        opt.PermitLimit = 5; // 5 intentos
        opt.Window = TimeSpan.FromMinutes(1); // por minuto
        opt.QueueLimit = 0;
    });
});

// =========================================================================
// ITEM 6: Response Compression (Brotli + Gzip)
// Concepto HTTP: Reduce el tamaño de transferencia mediante algoritmos de compresión.
// Brotli ofrece mayor tasa de compresión para texto que Gzip.
// =========================================================================
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/problem+json", "image/svg+xml" });
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// =========================================================================
// ITEM 7: Restrictive CORS Policy
// Concepto HTTP: Controla qué orígenes externos pueden realizar solicitudes cross-origin,
// gestionando peticiones preflight (OPTIONS).
// =========================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("RestrictiveCorsPolicy", policy =>
    {
        policy.WithOrigins("https://shortly.disc.cl")
              .WithMethods("GET", "POST")
              .WithHeaders("Content-Type", "Accept", "Authorization")
              .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});

// =========================================================================
// ITEM 14: Health Checks [BONUS]
// =========================================================================
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database_health_check");

builder.Services.AddAuthorization();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILinkRepository, LinkRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILinkService, LinkService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Pipeline de Middlewares en Orden de Ejecución Correcto
app.UseMiddleware<RequestTracingMiddleware>();    // Item #12: Tracing
app.UseMiddleware<PerformanceMiddleware>();       // Item #4: Latencia
app.UseMiddleware<SecurityHeadersMiddleware>();   // Item #3: Security Headers

app.UseResponseCompression();                      // Item #6: Compresión
app.UseStaticFiles();

app.UseRouting();

app.UseCors("RestrictiveCorsPolicy");             // Item #7: CORS
app.UseRateLimiter();                             // Item #5: Rate Limiting

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapOpenApi();
app.MapScalarApiReference();

app.MapUrlRedirect();

// =========================================================================
// ITEM 14: Health Check Endpoint [BONUS]
// =========================================================================
app.MapHealthChecks("/health", new()
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            uptime = Environment.TickCount64,
            checks = report.Entries.Select(e => new { key = e.Key, status = e.Value.Status.ToString() })
        });
        await context.Response.WriteAsync(result);
    }
});

// =========================================================================
// ITEM 15: Crawler Control Endpoints (robots.txt & sitemap.xml) [BONUS]
// Concepto HTTP: Instruit a web crawlers y bots no indexar las URLs acortadas.
// =========================================================================
app.MapGet("/robots.txt", () => Results.Text("User-agent: *\nDisallow: /", "text/plain"));
app.MapGet("/sitemap.xml", () => Results.Text("<?xml version=\"1.0\" encoding=\"UTF-8\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"></urlset>", "application/xml"));

// Inicialización de la BD
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    var seedPassword = app.Configuration["Seed:AdminPassword"] ?? "admin123";
    await DbInitializer.InitializeAsync(db, seedPassword);
}

await app.RunAsync();