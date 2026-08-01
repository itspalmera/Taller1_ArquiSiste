using Serilog.Context;

namespace Shortly.Middlewares;

/// <summary>
/// HTTP Concept - Tracing y Correlación de Solicitudes:
/// Propaga o genera un identificador único de correlación (X-Request-Id) a través del pipeline HTTP
/// y lo inyecta en el contexto de Serilog para rastrear peticiones en arquitectura distribuida.
/// </summary>
public class RequestTracingMiddleware
{
    private readonly RequestDelegate _next;
    private const string HeaderKey = "X-Request-Id";

    public RequestTracingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Usa el ID entrante si el cliente lo envió; de lo contrario genera uno nuevo
        string requestId = context.Request.Headers[HeaderKey].FirstOrDefault() ?? Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderKey] = requestId;

        // Inyecta RequestId en los logs estructurados de Serilog
        using (LogContext.PushProperty("RequestId", requestId))
        {
            await _next(context);
        }
    }
}