using System.Security.Cryptography;
using System.Text;
using Shortly.Application.Interfaces;

namespace Shortly.Endpoints;

public static class UrlRedirectEndpoint
{
    public static void MapUrlRedirect(this WebApplication app)
    {
        app.MapGet("/{shortUrl}", async (string shortUrl, HttpContext context, ILinkService linkService) =>
        {
            try
            {
                var link = await linkService.GetLink(shortUrl);

                // =========================================================================
                // ITEM 2: HTTP Response Caching & Conditional GET
                // Concepto HTTP: Los encabezados ETag y Last-Modified permiten validación condicional.
                // Si el cliente envía If-None-Match con el mismo ETag, se responde '304 Not Modified'
                // sin cuerpo de respuesta, ahorrando ancho de banda y procesamiento.
                // =========================================================================
                string etagValue = $"\"{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{link.ShortUrl}-{link.Url}-{link.Clicks}")))[..16]}\"";
                
                context.Response.Headers.CacheControl = "public, max-age=60";
                context.Response.Headers.ETag = etagValue;

                var ifNoneMatch = context.Request.Headers.IfNoneMatch.FirstOrDefault();
                if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch == etagValue)
                {
                    // HTTP 304 Not Modified: Ahorra ancho de banda al reutilizar la respuesta cacheada en cliente
                    return Results.StatusCode(StatusCodes.Status304NotModified);
                }

                await linkService.IncrementClicks(link.Id);

                // =========================================================================
                // ITEM 10: Conditional Redirect Status Codes
                // Concepto HTTP:
                // - HTTP 301 (Permanent Redirect): Indica a navegadores y c rawlers que la URL
                //   se movió permanentemente. Los clientes la cachean fuertemente.
                // - HTTP 307 (Temporary Redirect): Redirección temporal que preserva explícitamente
                //   el método HTTP original y la expiración.
                // =========================================================================
                if (link.Clicks > 100)
                {
                    // Enlaces estables con alto tráfico -> 301 Permanent Redirect
                    return Results.Redirect(link.Url, permanent: true, preserveMethod: false);
                }

                // Enlaces nuevos o temporales -> 307 Temporary Redirect
                return Results.Redirect(link.Url, permanent: false, preserveMethod: true);
            }
            catch (KeyNotFoundException)
            {
                // =========================================================================
                // ITEM 8: Content Negotiation for Errors (RFC 7807 Problem Details)
                // Concepto HTTP: Retorna una representación estructurada y estándar en JSON
                // (application/problem+json) para que los clientes machine-readable procesen
                // los errores adecuadamente según RFC 7807.
                // =========================================================================
                return Results.Problem(
                    title: "Short URL Not Found",
                    detail: $"The requested short URL '{shortUrl}' does not exist or has expired.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: new Dictionary<string, object?> { { "shortUrl", shortUrl } }
                );
            }
        });
    }
}