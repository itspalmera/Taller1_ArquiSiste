namespace Shortly.Middlewares;

/// <summary>
/// HTTP Concept - Security Headers:
/// Aplica defensas globales a nivel de navegador en cada respuesta HTTP para mitigar
/// vectores de ataque comunes (XSS, Clickjacking, MIME sniffing, Man-in-the-Middle).
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Mitiga Man-in-the-Middle forzando HTTPS durante 1 año (incluyendo subdominios)
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");

        // Previene MIME Sniffing: obliga al navegador a respetar el Content-Type declarado
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // Previene Clickjacking: prohíbe que el sitio sea incrustado en <frame>, <iframe> o <object>
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // Protege la privacidad: evita enviar la URL completa como Referer a sitios de terceros
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Restringe el acceso del navegador a características del dispositivo (cámara, micrófono, geolocalización)
        context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

        await _next(context);
    }
}