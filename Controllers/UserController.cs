using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoPrint.API.DTOs;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
[Produces("application/json")]
public sealed class UserController : ControllerBase
{
    /// <summary>
    /// Returns the authenticated user's profile and license details.
    /// All data is read from JWT claims — no database call needed.
    /// </summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Profile()
    {
        // Read claims set by JwtService.GenerateToken()
        var claims = User.Claims.ToDictionary(c => c.Type, c => c.Value);

        if (!claims.TryGetValue("license_expires", out var expiresStr) ||
            !claims.TryGetValue("license_issued",  out var issuedStr))
        {
            return Unauthorized(new ApiResponse<UserDto>(false, null, "Invalid token claims."));
        }

        var expiresAt   = DateTime.Parse(expiresStr, null, System.Globalization.DateTimeStyles.RoundtripKind);
        var issuedAt    = DateTime.Parse(issuedStr,  null, System.Globalization.DateTimeStyles.RoundtripKind);
        var now         = DateTime.UtcNow;
        var isExpired   = expiresAt < now;
        var isActive    = string.Equals(claims.GetValueOrDefault("license_active"), "true",
                            StringComparison.OrdinalIgnoreCase) && !isExpired;
        var daysLeft    = isExpired ? 0 : (int)(expiresAt - now).TotalDays;

        var licenseDto = new LicenseDto(
            IsActive:      isActive,
            IsExpired:     isExpired,
            Plan:          claims.GetValueOrDefault("license_plan", "standard"),
            IssuedAt:      issuedAt,
            ExpiresAt:     expiresAt,
            DaysRemaining: daysLeft
        );

        var userDto = new UserDto(
            Id:       claims.GetValueOrDefault("sub",       string.Empty),
            Email:    claims.GetValueOrDefault("email",     string.Empty),
            FullName: claims.GetValueOrDefault("full_name", string.Empty),
            License:  licenseDto
        );

        return Ok(new ApiResponse<UserDto>(true, userDto, null));
    }
}
