using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs;

namespace PhotoPrint.API.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public sealed class AuthService : IAuthService
{
    private readonly PhotoPrintDbContext _db;
    private readonly IJwtService        _jwt;

    public AuthService(PhotoPrintDbContext db, IJwtService jwt)
    {
        _db  = db;
        _jwt = jwt;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        // ── 1. Fetch user by email (case-insensitive) ─────────────────────
        var user = await _db.UsersWithLicenses()
            .FirstOrDefaultAsync(
                u => u.Email.ToLower() == request.Email.ToLower() && u.IsActive == true,
                ct);

        if (user is null)
            return Fail("Invalid email or password.");

        // ── 2. Verify password ────────────────────────────────────────────
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Fail("Invalid email or password.");

        // ── 3. Get most recent license ────────────────────────────────────
        var license = user.Licenses
            .OrderByDescending(l => l.ExpiresAt)
            .FirstOrDefault();

        if (license is null)
            return Fail("No license associated with this account. Contact your administrator.");

        // ── 4. Build license DTO ──────────────────────────────────────────
        var now        = DateTime.UtcNow;
        var isExpired = license.IsActive == true || license.ExpiresAt < now;
        var isActive   = license.IsActive == true && !isExpired;
        var daysLeft   = isExpired ? 0 : (int)(license.ExpiresAt - now).TotalDays;

        var licenseDto = new LicenseDto(
            IsActive:      isActive,
            IsExpired:     isExpired,
            Plan:          license.Plan,
            IssuedAt:      license.IssuedAt,
            ExpiresAt:     license.ExpiresAt,
            DaysRemaining: daysLeft
        );

        var userDto = new UserDto(
            Id:       user.Id.ToString(),
            Email:    user.Email,
            FullName: user.FullName,
            License:  licenseDto
        );

        // ── 5. Generate JWT ───────────────────────────────────────────────
        var token = _jwt.GenerateToken(user, license);

        return new LoginResponse(true, token, userDto, "Login successful.");
    }

    private static LoginResponse Fail(string msg)
        => new(false, null, null, msg);
}
