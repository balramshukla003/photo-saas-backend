using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public interface IJwtService
{
    string         GenerateToken(User user, License license);
    ClaimsPrincipal? ValidateToken(string token);
}

public sealed class JwtService : IJwtService
{
    private readonly SymmetricSecurityKey _key;
    private readonly string              _issuer;
    private readonly string              _audience;
    private readonly int                 _expiryHours;

    public JwtService(IConfiguration config)
    {
        var secret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        _key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        _issuer      = config["Jwt:Issuer"]   ?? "photoprint.app";
        _audience    = config["Jwt:Audience"] ?? "photoprint.users";
        _expiryHours = int.TryParse(config["Jwt:ExpiryHours"], out var h) ? h : 24;
    }

    // ── Generate ───────────────────────────────────────────────────────────
    public string GenerateToken(User user, License license)
    {
        var now = DateTime.UtcNow;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,   DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),

            // User claims
            new Claim("full_name",  user.FullName),

            // License claims — embedded so no extra DB call on subsequent requests
            new Claim("license_id",      license.Id.ToString()),
            new Claim("license_key",     license.LicenseKey),
            new Claim("license_active",  license.IsActive.ToString().ToLowerInvariant()),
            new Claim("license_plan",    license.Plan),
            new Claim("license_issued",  license.IssuedAt.ToString("O")),
            new Claim("license_expires", license.ExpiresAt.ToString("O")),
        };

        var token = new JwtSecurityToken(
            issuer:            _issuer,
            audience:          _audience,
            claims:            claims,
            notBefore:         now,
            expires:           now.AddHours(_expiryHours),
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── Validate ───────────────────────────────────────────────────────────
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = _key,
                    ValidateIssuer           = true,
                    ValidIssuer              = _issuer,
                    ValidateAudience         = true,
                    ValidAudience            = _audience,
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.Zero,
                },
                out _);
        }
        catch
        {
            return null;
        }
    }
}
