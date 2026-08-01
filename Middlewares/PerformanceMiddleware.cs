using System.Diagnostics;

namespace Shortly.Middlewares;

/// <summary>
/// HTTP Concept - Observabilidad de Latencia:
/// Mide la duración total del procesamiento de la solicitud HTTP en el pipeline del servidor,
/// adjunta el encabezado personalizado `X-Response-Time` y emite logs de advertencia para peticiones lentas (>500ms).
/// </summary>
public class PerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMiddleware> _logger;

    public PerformanceMiddleware(RequestDelegate next, ILogger<PerformanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        context.Response.OnStarting(() =>
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            context.Response.Headers["X-Response-Time"] = $"{elapsedMs}ms";

            if (elapsedMs > 500)
            {
                // Log específico para diagnóstico y monitoreo de endpoints lentos
                _logger.LogWarning(
                    "SLOW REQUEST DETECTED: {Method} {Path} responded with status {StatusCode} in {Elapsed}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    elapsedMs);
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}