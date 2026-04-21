// ─────────────────────────────────────────────────────────────────────────────
//  DTOs — safe to edit, never touched by scaffold
// ─────────────────────────────────────────────────────────────────────────────

namespace PhotoPrint.API.DTOs;

// ── Auth ──────────────────────────────────────────────────────────────────────
public sealed record LoginRequest(
    string Email,
    string Password
);

public sealed record LoginResponse(
    bool   Success,
    string? Token,
    UserDto? User,
    string? Message
);

// ── User ──────────────────────────────────────────────────────────────────────
public sealed record UserDto(
    string     Id,
    string     Email,
    string     FullName,
    LicenseDto License
);

// ── License ───────────────────────────────────────────────────────────────────
public sealed record LicenseDto(
    bool     IsActive,
    bool     IsExpired,
    string   Plan,
    DateTime IssuedAt,
    DateTime ExpiresAt,
    int      DaysRemaining
);

// ── Photo ─────────────────────────────────────────────────────────────────────
public sealed record PhotoProcessResponse(
    bool    Success,
    string? ProcessedImageBase64,
    string? MimeType,
    string? Message
);

// ── Generic API wrapper ───────────────────────────────────────────────────────
public sealed record ApiResponse<T>(
    bool    Success,
    T?      Data,
    string? Message
);
