namespace PhotoPrint.API.Middleware;

/// <summary>
/// Validates that the authenticated user's license is still active and not expired.
/// Runs after UseAuthentication() — claims are already populated by JWT middleware.
/// Applies only to routes under /api/photo and /api/user.
/// </summary>
public sealed class LicenseMiddleware
{
    private static readonly string[] ProtectedPrefixes =
    [
        "/api/photo",
        "/api/user",
    ];

    private readonly RequestDelegate          _next;
    private readonly ILogger<LicenseMiddleware> _logger;

    public LicenseMiddleware(RequestDelegate next, ILogger<LicenseMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path        = context.Request.Path.Value ?? string.Empty;
        var isProtected = ProtectedPrefixes.Any(p =>
            path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (isProtected && context.User.Identity?.IsAuthenticated == true)
        {
            var licenseActiveStr = context.User.FindFirst("license_active")?.Value;
            var licenseExpiresStr = context.User.FindFirst("license_expires")?.Value;

            var isActive = string.Equals(licenseActiveStr, "true",
                StringComparison.OrdinalIgnoreCase);

            var isExpired = licenseExpiresStr is not null
                && DateTime.TryParse(licenseExpiresStr, null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var expiresAt)
                && expiresAt < DateTime.UtcNow;

            if (!isActive || isExpired)
            {
                _logger.LogWarning(
                    "License check failed for user {UserId}. Active={Active}, Expired={Expired}",
                    context.User.FindFirst("sub")?.Value,
                    isActive,
                    isExpired);

                context.Response.StatusCode  = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsync(
                    """{"success":false,"message":"License expired or inactive. Contact your administrator to renew."}""",
                    context.RequestAborted);

                return;
            }
        }

        await _next(context);
    }
}
