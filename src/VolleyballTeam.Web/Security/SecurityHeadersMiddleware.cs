namespace VolleyballTeam.Web.Security;

public class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; " +
        "font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        // Development hot reload injects scripts and a websocket that this policy would block.
        if (!environment.IsDevelopment())
        {
            headers.ContentSecurityPolicy = ContentSecurityPolicy;
        }

        // Pages for signed-in users hold minors' details, so shared or back-button caches must not keep them.
        context.Response.OnStarting(() =>
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                context.Response.Headers.CacheControl = "no-store";
            }

            return Task.CompletedTask;
        });

        return next(context);
    }
}
