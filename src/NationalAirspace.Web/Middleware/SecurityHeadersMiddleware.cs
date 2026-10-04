namespace NationalAirspace.Web.Middleware;

/// <summary>Adds defensive HTTP headers, including a Content-Security-Policy without inline scripts.</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https://i.ytimg.com; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "frame-src https://www.youtube-nocookie.com https://player.vimeo.com https://www.google.com https://maps.google.com https://www.openstreetmap.org; " +
        "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; upgrade-insecure-requests";

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Content-Security-Policy"] = ContentSecurityPolicy;
            if (context.Request.Path.StartsWithSegments("/Admin"))
                headers["Cache-Control"] = "no-store";
            return Task.CompletedTask;
        });
        return next(context);
    }
}
